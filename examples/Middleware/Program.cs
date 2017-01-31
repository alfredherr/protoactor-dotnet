using System;
using System.Collections.Generic;
using Proto;

class Program
{
    static void Main(string[] args)
    {
        var actor = Actor.FromFunc(c =>
            {
                Console.WriteLine($"actor got {c.Message.GetType()}:{c.Message}");
                return Actor.Done;
            })
            .WithMiddleware(
                next => async c =>
                {
                    Console.WriteLine($"middleware 1 enter {c.Message.GetType()}:{c.Message}");
                    await next(c);
                    Console.WriteLine($"middleware 1 exit");
                },
                next => async c =>
                {
                    Console.WriteLine($"middleware 2 enter {c.Message.GetType()}:{c.Message}");
                    await next(c);
                    Console.WriteLine($"middleware 2 exit");
                });

        var pid = Actor.Spawn(actor);
        pid.Tell("hello");
        Console.ReadLine();
        Console.ReadLine();
    }
}
