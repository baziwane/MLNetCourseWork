﻿using System;
using Microsoft.ML;
using System.IO;
using Microsoft.ML.Data;

namespace BikeDemandPrediction
{
    class Program
    {
        // filename for data set
        private static string dataPath = Path.Combine(Environment.CurrentDirectory, "bikedemand.csv");
        static void Main(string[] args)
        {
            // create the machine learning context
            var context = new MLContext();

            // load the dataset
            Console.WriteLine("Loading data...");
            var data = context.Data.LoadFromTextFile<DemandObservation>(
                path: dataPath,
                hasHeader: true,
                separatorChar: ',');

            // split the dataset into 80% training and 20% testing
            var partitions = context.Data.TrainTestSplit(data, testFraction: 0.2);
            // build a training pipeline
            // step 1: concatenate all feature columns
            var pipeline = context.Transforms.Concatenate(
                "Features",
                nameof(DemandObservation.Season),
                nameof(DemandObservation.Year),
                nameof(DemandObservation.Month),
                nameof(DemandObservation.Hour),
                nameof(DemandObservation.Holiday),
                nameof(DemandObservation.Weekday),
                nameof(DemandObservation.WorkingDay),
                nameof(DemandObservation.Weather),
                nameof(DemandObservation.Temperature),
                nameof(DemandObservation.NormalizedTemperature),
                nameof(DemandObservation.Humidity),
                nameof(DemandObservation.Windspeed))

                // step 2: cache the data to speed up training
                .AppendCacheCheckpoint(context)

                // step 3: use a fast forest learner
                .Append(context.Regression.Trainers.FastForest(
                    labelColumnName: "Label",
                    featureColumnName: "Features",
                    numberOfLeaves: 20,
                    numberOfTrees: 100,
                    minimumExampleCountPerLeaf: 10));

            // train the model
            Console.WriteLine("Training the model...");
            var trainedModel = pipeline.Fit(partitions.TrainSet);

            // evaluate the model
            Console.WriteLine("Evaluating the model...");
            var predictions = trainedModel.Transform(partitions.TestSet);
            var metrics = context.Regression.Evaluate(
                data: predictions,
                labelColumnName: "Label",
                scoreColumnName: "Score");

            // show evaluation metrics
            Console.WriteLine($"   RMSE: {metrics.RootMeanSquaredError}");
            Console.WriteLine($"   MSE:  {metrics.MeanSquaredError}");
            Console.WriteLine($"   MAE:  {metrics.MeanAbsoluteError}");

            // set up a sample observation
            var sample = new DemandObservation()
            {
                Season = 3,
                Year = 1,
                Month = 8,
                Hour = 10,
                Holiday = 0,
                Weekday = 4,
                WorkingDay = 1,
                Weather = 1,
                Temperature = 0.8f,
                NormalizedTemperature = 0.7576f,
                Humidity = 0.55f,
                Windspeed = 0.2239f
            };

            // create a prediction engine
            var engine = context.Model.CreatePredictionEngine<DemandObservation, DemandPrediction>(trainedModel);

            // make the prediction
            Console.WriteLine("Making a prediction...");
            var prediction = engine.Predict(sample);

            // show the prediction
            Console.WriteLine($"   {prediction.PredictedCount}");
        }
    }
}
