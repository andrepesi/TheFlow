﻿using System;
using System.Linq;
using FluentAssertions;
using TheFlow.CoreConcepts;
using TheFlow.Elements.Activities;
using TheFlow.Infrastructure.Stores;
using Xunit;

namespace TheFlow.Tests.Functional.Basics
{
    public class Transactions
    {
        [Fact]
        public void WhenTheProcessFailsCompensationActivitiesRun()
        {
            var data = 0;
            var model = ProcessModel.Create()
                .AddAnyEventCatcher("start")
                .AddActivity("regular", () => data = 10)
                .AddActivity("compensation", () => data -= 5)
                .AttachAsCompensationActivity("compensation", "regular")
                .AddActivity("failing", () => throw new Exception())
                .AddEventThrower("end")
                .AddSequenceFlow("start", "regular", "failing", "end");

            var models = new InMemoryProcessModelsStore(model);
            var instances = new InMemoryProcessInstancesStore();

            var manager = new ProcessManager(models, instances);

            manager.HandleEvent(null);
            data.Should().Be(5);
        }

        public static int SharedState = 0;
        class RegularActivity : Activity
        {
            public override void Run(ExecutionContext context)
            {
                SharedState = 10;
                context.Instance.HandleActivityCompletion(context, null);
            }
        }

        class CompensationActivity : Activity
        {
            public override void Run(ExecutionContext context)
            {
                SharedState -= 5;
                context.Instance.HandleActivityCompletion(context, null);
            }
        }

        [Fact]
        public void WhenTheProcessFailsCompensationActivitiesRun2()
        {
            var model = ProcessModel.Create()
                .AddAnyEventCatcher("start")
                .AddActivityWithCompensation<RegularActivity, CompensationActivity>()
                .AddActivity("failing", () => throw new Exception())
                .AddEventThrower("end")
                .AddSequenceFlow("start", "Regular", "failing", "end");

            var models = new InMemoryProcessModelsStore(model);
            var instances = new InMemoryProcessInstancesStore();

            var manager = new ProcessManager(models, instances);

            manager.HandleEvent(null);
            SharedState.Should().Be(5);
        }



        [Fact]
        public void WhenManualActivityFailsCompensationActivitiesRun()
        {
            var data = 0;
            var model = ProcessModel.Create()
                .AddAnyEventCatcher("start")
                .AddActivity("regular", () => data = 10)
                .AddActivity("compensation", () => data -= 5)
                .AttachAsCompensationActivity("compensation", "regular")
                .AddActivity<ManualActivity>("failing")
                .AddEventThrower("end")
                .AddSequenceFlow("start", "regular", "failing", "end");

            var models = new InMemoryProcessModelsStore(model);
            var instances = new InMemoryProcessInstancesStore();

            var manager = new ProcessManager(models, instances);

            var result = manager.HandleEvent(null).First();

            manager.HandleActivityFailure(
                result.ProcessInstanceId,
                result.AffectedTokens.First(),
                null);

            data.Should().Be(5);
        }

        [Fact]
        public void WhenProcessFailCompensationsAreExecutedOnlyForActivitiesThatWerePerformed()
        {
            var e1 = false;
            var e2 = false;
            var e3 = false;

            var model = ProcessModel.Create()
                .AddAnyEventCatcher("start")
                .AddActivity("a1", () => { })
                .AddActivity("c1", () => e1 = true)
                .AttachAsCompensationActivity("c1", "a1")
                .AddActivity("a2", () => throw new Exception())
                .AddActivity("c2", () => e2 = true)
                .AttachAsCompensationActivity("c2", "a2")
                .AddActivity("a3", () => { })
                .AddActivity("c3", () => e3 = true)
                .AttachAsCompensationActivity("c3", "a3")
                .AddEventThrower("end")
                .AddSequenceFlow("start", "a1", "a2", "a3", "end");

            var models = new InMemoryProcessModelsStore(model);
            var instances = new InMemoryProcessInstancesStore();

            var manager = new ProcessManager(models, instances);

            var result = manager.HandleEvent(null).First();
            var relatedInstance = manager.InstancesStore.GetById(result.ProcessInstanceId);

            relatedInstance.WasActivityCompleted("a1").Should().BeTrue();
            relatedInstance.WasActivityCompleted("a2").Should().BeFalse();
            relatedInstance.WasActivityCompleted("a3").Should().BeFalse();

            e1.Should().BeTrue();
            e2.Should().BeFalse();
            e3.Should().BeFalse();
        }

    }
}
