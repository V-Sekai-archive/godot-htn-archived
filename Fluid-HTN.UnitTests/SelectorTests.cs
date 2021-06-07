﻿using FluidHTN.Compounds;
using FluidHTN.Conditions;
using FluidHTN.PrimitiveTasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluid_HTN.UnitTests
{
    [TestClass]
    public class SelectorTests
    {
        [TestMethod]
        public void AddCondition_ExpectedBehavior()
        {
            var task = new Selector() { Name = "Test" };
            var t = task.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == false));

            Assert.IsTrue(t == task);
            Assert.IsTrue(task.Conditions.Count == 1);
        }

        [TestMethod]
        public void AddSubtask_ExpectedBehavior()
        {
            var task = new Selector() { Name = "Test" };
            var t = task.AddSubtask(new PrimitiveTask() { Name = "Sub-task" });

            Assert.IsTrue(t == task);
            Assert.IsTrue(task.Subtasks.Count == 1);
        }

        [TestMethod]
        public void IsValidFailsWithoutSubtasks_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };

            Assert.IsFalse(task.IsValid(ctx));
        }

        [TestMethod]
        public void IsValid_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task" });

            Assert.IsTrue(task.IsValid(ctx));
        }

        [TestMethod]
        public void DecomposeWithNoSubtasks_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Failed);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
        }

        [TestMethod]
        public void DecomposeWithSubtasks_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" });
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task1", plan.Peek().Name);
        }

        [TestMethod]
        public void DecomposeWithSubtasks2_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            task.AddSubtask(new Selector() { Name = "Sub-task1" });
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task2", plan.Peek().Name);
        }

        [TestMethod]
        public void DecomposeWithSubtasks3_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task2", plan.Peek().Name);
        }

        [TestMethod]
        public void DecomposeMTRFails_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });
            ctx.LastMTR.Add(0);
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 1);
            Assert.AreEqual(-1, ctx.MethodTraversalRecord[0]);
        }

        [TestMethod]
        public void DecomposeDebugMTRFails_ExpectedBehavior()
        {
            var ctx = new MyDebugContext();
            ctx.Init();

            var task = new Selector() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });
            ctx.LastMTR.Add(0);
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MTRDebug.Count == 1);
            Assert.IsTrue(ctx.MTRDebug[0].Contains("REPLAN FAIL"));
            Assert.IsTrue(ctx.MTRDebug[0].Contains("Sub-task2"));
        }

        [TestMethod]
        public void DecomposeMTRSucceedsWhenEqual_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });
            ctx.LastMTR.Add(1);
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 0);
            Assert.IsTrue(plan.Count == 1);
        }

        [TestMethod]
        public void DecomposeCompoundSubtaskSucceeds_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" });

            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task2", plan.Peek().Name);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 1);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
        }

        [TestMethod]
        public void DecomposeCompoundSubtaskFails_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" });

            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task3", plan.Peek().Name);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 0);
        }

        [TestMethod]
        public void DecomposeNestedCompoundSubtaskFails_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new Selector() { Name = "Test3" };
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));

            task2.AddSubtask(task3);
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task4" });

            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task4", plan.Peek().Name);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 0);
        }

        [TestMethod]
        public void DecomposeCompoundSubtaskBeatsLastMTR_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" });

            ctx.LastMTR.Add(1);
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task2", plan.Peek().Name);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 1);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
        }

        [TestMethod]
        public void DecomposeCompoundSubtaskEqualToLastMTR_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" });

            ctx.LastMTR.Add(0);
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task2", plan.Peek().Name);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 1);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
        }

        [TestMethod]
        public void DecomposeCompoundSubtaskLoseToLastMTR_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Selector() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });

            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task.AddSubtask(task2);

            ctx.LastMTR.Add(0);
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 1);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == -1);
        }

        [TestMethod]
        public void DecomposeCompoundSubtaskWinOverLastMTR_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var rootTask = new Selector() { Name = "Root" };
            var task = new Selector() { Name = "Test1" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new Selector() {Name = "Test3"};

            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task3-1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task3-2" });

            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task2-1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task2-2" });

            task.AddSubtask(task2);
            task.AddSubtask(task3);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1-1" }.AddCondition(new FuncCondition<MyContext>("Done == false", context => context.Done == false)));

            rootTask.AddSubtask(task);

            ctx.LastMTR.Add(0);
            ctx.LastMTR.Add(1);
            ctx.LastMTR.Add(0);

            // In this test, we prove that [0, 0, 1] beats [0, 1, 0]
            var status = rootTask.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
        }

        [TestMethod]
        public void DecomposeCompoundSubtaskLoseToLastMTR2_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var rootTask = new Selector() { Name = "Root" };
            var task = new Selector() { Name = "Test1" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new Selector() { Name = "Test3" };

            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task2-1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task2-1" });

            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1-1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task.AddSubtask(task2);

            rootTask.AddSubtask(task);

            ctx.LastMTR.Add(0);
            ctx.LastMTR.Add(1);
            ctx.LastMTR.Add(0);

            // We expect this test to be rejected, because [0,1,1] shouldn't beat [0,1,0]
            var status = rootTask.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 3);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == 1);
            Assert.IsTrue(ctx.MethodTraversalRecord[2] == -1);
        }
    }
}
