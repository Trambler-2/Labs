﻿using System;
using System.Collections.Generic;
using System.Linq;

using MathNet.Numerics.Distributions;

using Library.Graph.Types;
using Library.Graph.Generators.Options;

namespace Library.Graph.Generators
{
    public abstract class GraphGenerator<TGraph, TViewItem, TValue, TOptions> : IGraphGenerator<TGraph, TViewItem, TValue>
        where TValue : notnull
        where TViewItem : IGraphViewItem<TValue>
        where TGraph : IGraph<TViewItem, TValue>
        where TOptions : GeneratorGraphOptions<TValue>
    {
        public IRandomizer Randomizer { get; } = DefaultRandomizer.Randomizer;

        public IDistributionCalculator DistributionCalculator { get; private set; }

        public GraphGenerator(TOptions options, IRandomizer? randomizer = null, IDistributionCalculator? distributionCalculator = null)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            if (randomizer is not null)
            {
                Randomizer = randomizer;
            }
            if (distributionCalculator is not null)
            {
                DistributionCalculator = distributionCalculator;
            }
            else
            {
                DistributionCalculator = new DefaultDistributionCalculator(options.MeanConnectivity);
            }
        }

        public GraphGeneratingResult<TGraph, TViewItem, TValue> Generate()
        {
            Prepare();
            return BuildCore();
        }

        protected TOptions Options { get; set; }

        protected Dictionary<TValue, (int Count, HashSet<TValue> Items)> MapVertexAndLists { get; private set; } = new ();

        protected abstract GraphGeneratingResult<TGraph, TViewItem, TValue> BuildCore();

        protected virtual void Prepare()
        {
            MapVertexAndLists = new();

            var anomalyDetected = 10_000_000;
            while (MapVertexAndLists.Count != Options.VerticesCount)
            {
                if (anomalyDetected-- == 0)
                {
                    throw new InvalidOperationException("Detected anomaly, cause received bad vertex factory.");
                }
                var vertex = Options.VerticiesFactory();
                if (!MapVertexAndLists.ContainsKey(vertex))
                {
                    var elements = Poisson.Sample(Options.MeanConnectivity);

                    elements = elements <= 0 ? 1 : (elements >= Options.VerticesCount ? Options.VerticesCount - 1 : elements);

                    MapVertexAndLists.Add(vertex, (Count: elements, Items: new HashSet<TValue>()));
                }
            }
        }

        protected bool IsLoop(TValue vertexFrom, TValue vertexTo)
            => vertexFrom.Equals(vertexTo);

        protected bool IsContainsDuplicate(TValue vertex, IEnumerable<TValue> items)
            => items.Contains(vertex);

        protected TValue GetRandomVertexFrom(IReadOnlyList<TValue> vertices)
            => vertices[Randomizer.FromRange(vertices.Count)];
    }
}