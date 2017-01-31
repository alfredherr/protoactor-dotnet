﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Core;
using Couchbase.Core.Serialization;
using Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Proto;
using Proto.Persistence;
using Proto.Persistence.Couchbase;

class Program
{
    class MyActor : IPersistentActor
    {
        public Persistence Persistence { get; set; }

        private State _state = new State();

        public Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case RenameCommand rc:
                    Persistence.PersistReceive(new RenameEvent { Name = rc.Name });
                    break;
                case RenameEvent re:
                    _state.Name = re.Name;
                    Console.WriteLine($"{context.Self.Id} changed name to {_state.Name}");
                    break;
                case RequestSnapshot rs:
                    Persistence.PersistSnapshot(_state);
                    break;
                case State s:
                    _state = s;
                    break;
            }
            return Actor.Done;
        }
    }

    static void Main(string[] args)
    {
        using (var cluster = GetCluster())
        using (var bucket = cluster.OpenBucket("protoactor-test"))
        {
            var provider = new CouchbaseProvider(bucket, 5);
            var props = Actor.FromProducer(() => new MyActor())
                .WithMiddleware(Persistence.Using(provider));
            var pid = Actor.Spawn(props);
            pid.Tell(new RenameCommand { Name = "Christian" });
            pid.Tell(new RenameCommand { Name = "Alex" });
            pid.Tell(new RenameCommand { Name = "Roger" });
            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
    }

    private static ICluster GetCluster()
    {
        var clientDefinition = new CouchbaseClientDefinition
        {
            Buckets = new List<BucketDefinition>
            {
                new BucketDefinition
                {
                    Name = "protoactor-test",
                    ConnectionPool = new ConnectionPoolDefinition
                    {
                        EnableTcpKeepAlives = true,
                        MaxSize = 100,
                        MinSize = 10,
                    },
                }
            },
            Servers = new List<Uri>
            {
                new Uri("http://localhost:8091")
            },
        };
        var jsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        var configuration = new ClientConfiguration(clientDefinition)
        {
            Serializer = () => new DefaultSerializer(jsonSerializerSettings, jsonSerializerSettings)
        };
        return new Cluster(configuration);
    }
}