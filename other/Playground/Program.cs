﻿using System;
using TheFlow;
using TheFlow.CoreConcepts;
using TheFlow.Elements.Activities;
using TheFlow.Elements.Events;
using TheFlow.Infrastructure.Stores;

namespace Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            var model = ProcessModel.Create()
                .AddEventCatcher("start")
                .AddActivity("msgBefore", LambdaActivity.Create(() => {Console.WriteLine("Before");}))
                .AddParallelGateway("split")
                .AddSequenceFlow("start", "msgBefore", "split")
                .AddActivity("msgLeft", LambdaActivity.Create(() => {Console.WriteLine("Left");}))
                .AddActivity("msgRight", LambdaActivity.Create(() => {Console.WriteLine("Right");}))
                .AddParallelGateway("join")
                .AddSequenceFlow("split", "msgLeft", "join")
                .AddSequenceFlow("split", "msgRight", "join")
                .AddActivity("msgAfter", LambdaActivity.Create(() => {Console.WriteLine("After");}))
                .AddEventThrower("end")
                .AddSequenceFlow("join", "msgAfter", "end");

            var models = new InMemoryProcessModelsStore(model);
            var instances = new InMemoryProcessInstancesStore();
            
            var manager = new ProcessManager(models, instances);

            manager.HandleEvent(null);
        }
    }
}
