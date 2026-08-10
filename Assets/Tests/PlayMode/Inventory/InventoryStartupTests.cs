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
            Assert.That(inventory.Snapshot.Capacity, Is.EqualTo(24));
            Assert.That(
                inventory.GetTotalQuantity("training_chip"),
                Is.EqualTo(8));
            Assert.That(
                inventory.GetTotalQuantity("city_token"),
                Is.EqualTo(120));
        }
    }
}
