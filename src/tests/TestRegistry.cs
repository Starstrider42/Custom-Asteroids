using NUnit.Framework;
using System;


namespace Starstrider42.CustomAsteroids
{
	static class TestUtils
	{
		public static ConfigNode makeVesselConfig (uint id)
		{
			var config = new ConfigNode ("VESSEL");
			config.AddNode ("PART");
			config.AddValue ("persistentId", id);
			return config;
		}

		public static ConfigNode makeCorruptedVesselConfig ()
		{
			var config = new ConfigNode ("VESSEL");
			config.AddNode ("PART");
			config.AddValue ("persistentId", "Not a Number");
			return config;
		}

		public static AsteroidInfo makeAsteroidInfo ()
		{
			ConfigNode config = makeVesselConfig (42);
			return new AsteroidInfo (config, "fakePopulation");
		}
	}

	/**
	 * Neither ScenarioModules nor Vessels can be created outside Unity, so no practical
	 * way to unit-test CustomAsteroidRegistry.
	 */

	[TestFixture]
	public sealed class TestAsteroidInfo
	{
		[Test]
		public void TestConstructorValid ()
		{
			uint id = 101;
			string group = "potatoroids";
			var newTestbed = new AsteroidInfo (TestUtils.makeVesselConfig (id), group);
			Assert.AreEqual (newTestbed.id, id);
			Assert.AreEqual (newTestbed.parentSet, group);
		}

		[Test]
		public void TestConstructorNoId ()
		{
			Assert.Catch<ArgumentException> (() => {
				var foo = new AsteroidInfo (new ConfigNode (), "potatoroids");
			});
		}

		[Test]
		public void TestConstructorBadId ()
		{
			Assert.Catch<ArgumentException> (() => {
				var foo = new AsteroidInfo (TestUtils.makeCorruptedVesselConfig (), "potatoroids");
			});
		}

		[Test]
		public void TestConstructorNullVessel ()
		{
			Assert.Catch<ArgumentNullException> (() => {
				var foo = new AsteroidInfo ((ConfigNode)null, "something");
			});
			Assert.Catch<ArgumentNullException> (() => {
				var foo = new AsteroidInfo ((ProtoVessel)null, "something");
			});
			Assert.Catch<ArgumentNullException> (() => {
				var foo = new AsteroidInfo ((Vessel)null, "something");
			});
		}

		[Test]
		public void TestConstructorNullGroup ()
		{
			var testbed = new AsteroidInfo (TestUtils.makeVesselConfig (42), null);
			Assert.IsNull (testbed.parentSet);
		}

		[Test]
		public void TestLoadSave ()
		{
			var testbed = TestUtils.makeAsteroidInfo ();
			// NUnit doesn't seem to have a "requires"
			Assert.AreNotEqual (default (uint), testbed.id);
			Assert.AreNotEqual (null, testbed.parentSet);

			ConfigNode saved = testbed.ToConfigNode ();
			AsteroidInfo copy = AsteroidInfo.FromConfigNode (saved);
			Assert.AreEqual (testbed.id, copy.id);
			Assert.AreEqual (testbed.parentSet, copy.parentSet);
		}
	}

}
