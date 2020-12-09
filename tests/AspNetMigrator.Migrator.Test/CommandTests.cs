﻿using System;
using System.Threading;
using System.Threading.Tasks;
using AspNetMigrator.Commands;
using AspNetMigrator.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AspNetMigrator.Test
{
    [TestClass]
    public class CommandTests
    {
        [TestMethod]
        public async Task NegativeTests()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new ApplyNextCommand(null!));
            Assert.ThrowsException<ArgumentNullException>(() => new SkipNextCommand(null!));
            Assert.ThrowsException<ArgumentNullException>(() => new ExitCommand(null!));

            // Applying before intialization throws
            using var context = new NullMigrationContext();
            var step = new TestMigrationStep(string.Empty);
            var command = new ApplyNextCommand(step);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await command.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ApplyNextCommandAppliesSteps()
        {
            using var context = new NullMigrationContext();
            var stepTitle = "Test step!";
            var step = new TestMigrationStep(stepTitle);
            var command = new ApplyNextCommand(step);

            // Initialize step
            Assert.AreEqual(MigrationStepStatus.Unknown, step.Status);
            await step.InitializeAsync(context, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(MigrationStepStatus.Incomplete, step.Status);

            // Apply command
            Assert.AreEqual(0, step.ApplicationCount);
            Assert.IsTrue(await command.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false));

            // Confirm command text and step state are as expected
            Assert.AreEqual($"Apply next step ({stepTitle})", command.CommandText);
            Assert.AreEqual(1, step.ApplicationCount);
            Assert.AreEqual(MigrationStepStatus.Complete, step.Status);
            Assert.AreEqual(step.AppliedMessage, step.StatusDetails);

            // Confirm steps are only applied once
            Assert.IsTrue(await command.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false));
            Assert.AreEqual(1, step.ApplicationCount);
        }

        [TestMethod]
        public async Task FailedStepsAreNotApplied()
        {
            using var context = new NullMigrationContext();
            var stepTitle = "Failed test step!";
            var step = new FailedTestMigrationStep(stepTitle);
            var command = new ApplyNextCommand(step);

            await step.InitializeAsync(context, CancellationToken.None).ConfigureAwait(false);

            // Apply command
            Assert.AreEqual(0, step.ApplicationCount);
            Assert.IsFalse(await command.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false));

            // Confirm step was not applied
            Assert.AreEqual($"Apply next step ({stepTitle})", command.CommandText);
            Assert.AreEqual(0, step.ApplicationCount);
            Assert.AreEqual(MigrationStepStatus.Failed, step.Status);
            Assert.AreEqual(step.InitializedMessage, step.StatusDetails);
        }

        [TestMethod]
        public async Task SkipNextCommandSkips()
        {
            using var context = new NullMigrationContext();
            var stepTitle = "Test step!";
            var step = new TestMigrationStep(stepTitle);
            var command = new SkipNextCommand(step);

            // Initialize step
            Assert.AreEqual(MigrationStepStatus.Unknown, step.Status);
            await step.InitializeAsync(context, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(MigrationStepStatus.Incomplete, step.Status);

            // Apply command
            Assert.IsTrue(await command.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false));

            // Confirm command text and step state are as expected
            Assert.AreEqual($"Skip next step ({stepTitle})", command.CommandText);
            Assert.AreEqual(0, step.ApplicationCount);
            Assert.AreEqual(MigrationStepStatus.Skipped, step.Status);
            Assert.AreEqual("Step skipped", step.StatusDetails);
        }

        [TestMethod]
        public async Task ExitCommandExits()
        {
            using var context = new NullMigrationContext();
            var exitCalled = false;
            var command = new ExitCommand(() => exitCalled = true);

            Assert.AreEqual("Exit", command.CommandText);
            Assert.IsFalse(exitCalled);
            Assert.IsTrue(await command.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false));
            Assert.IsTrue(exitCalled);
        }
    }
}