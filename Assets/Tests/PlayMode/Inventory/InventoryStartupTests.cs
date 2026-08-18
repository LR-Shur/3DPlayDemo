using System.Collections;
using NUnit.Framework;
using Train.Architecture.Bootstrap;
using Train.Inventory.Application;
using UnityEngine;
using UnityEngine.TestTools;

namespace Train.Tests.PlayMode.Inventory
{
    public sealed class InventoryStartupTests
    {
        [UnityTest]
        public IEnumerator Startup_LoadsSettingsAndInstallsInventoryService()
        {
            IInventoryService inventory = null;
            var deadline = Time.realtimeSinceStartup + 15f;

            while (Time.realtimeSinceStartup < deadline &&
                   !GameBootstrap.Instance.Context.Services.TryResolve(
                       out inventory))
            {
                yield return null;
            }

            Assert.That(
                inventory,
                Is.Not.Null,
                "GameApplicationStartup did not install IInventoryService.");
            Assert.That(inventory.Snapshot.Capacity, Is.EqualTo(36));
            Assert.That(
                inventory.TryGetDefinition("training_chip", out var trainingChip),
                Is.True);
            Assert.That(
                trainingChip,
                Is.Not.Null);
            Assert.That(
                inventory.TryGetDefinition("city_token", out var cityToken),
                Is.True);
            Assert.That(
                cityToken,
                Is.Not.Null);
        }
    }
}
