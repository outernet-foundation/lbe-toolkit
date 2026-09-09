using System.IO;
using NUnit.Framework;
using UnityEngine;
using Outernet.LBEToolkit.StateSynchronization;

namespace Outernet.LBEToolkit.StateSynchronization.Tests
{
    public class SerializationTests
    {
        [Test]
        public void TestSerializeDeserialize()
        {
            bool boolVar = true;
            int intVar = 22;
            float floatVar = 0.33f;
            string stringVar = "cat";
            byte byteVar = 2;
            Vector2 vector2Var = new Vector2(1, -2);
            Vector3 vector3Var = new Vector3(1, 0, -2);
            Vector4 vector4Var = new Vector4(1, -2, 0, 0.3f);
            Quaternion quaternionVar = Quaternion.Euler(90f, 30f, 15f);
            Color colorVar = new Color(1, 2, 0, 0.33f);

            byte[] data = null;

            using (var serializationStream = new MemoryStream())
            using (var writer = new BinaryWriter(serializationStream))
            {
                Serialization.GetSerializer(typeof(bool)).Serialize(writer, boolVar, false);
                Serialization.GetSerializer(typeof(int)).Serialize(writer, intVar, false);
                Serialization.GetSerializer(typeof(float)).Serialize(writer, floatVar, false);
                Serialization.GetSerializer(typeof(string)).Serialize(writer, stringVar, false);
                Serialization.GetSerializer(typeof(byte)).Serialize(writer, byteVar, false);
                Serialization.GetSerializer(typeof(Vector2)).Serialize(writer, vector2Var, false);
                Serialization.GetSerializer(typeof(Vector3)).Serialize(writer, vector3Var, false);
                Serialization.GetSerializer(typeof(Vector4)).Serialize(writer, vector4Var, false);
                Serialization.GetSerializer(typeof(Quaternion)).Serialize(writer, quaternionVar, false);
                Serialization.GetSerializer(typeof(Color)).Serialize(writer, colorVar, false);
                data = serializationStream.ToArray();
            }

            using (var deserializationStream = new MemoryStream(data))
            using (var reader = new BinaryReader(deserializationStream))
            {
                Assert.AreEqual(boolVar, Serialization.GetSerializer(typeof(bool)).Deserialize(reader, false));
                Assert.AreEqual(intVar, Serialization.GetSerializer(typeof(int)).Deserialize(reader, false));
                Assert.AreEqual(floatVar, Serialization.GetSerializer(typeof(float)).Deserialize(reader, false));
                Assert.AreEqual(stringVar, Serialization.GetSerializer(typeof(string)).Deserialize(reader, false));
                Assert.AreEqual(byteVar, Serialization.GetSerializer(typeof(byte)).Deserialize(reader, false));
                Assert.AreEqual(vector2Var, Serialization.GetSerializer(typeof(Vector2)).Deserialize(reader, false));
                Assert.AreEqual(vector3Var, Serialization.GetSerializer(typeof(Vector3)).Deserialize(reader, false));
                Assert.AreEqual(vector4Var, Serialization.GetSerializer(typeof(Vector4)).Deserialize(reader, false));
                Assert.AreEqual(quaternionVar, Serialization.GetSerializer(typeof(Quaternion)).Deserialize(reader, false));
                Assert.AreEqual(colorVar, Serialization.GetSerializer(typeof(Color)).Deserialize(reader, false));
            }
        }

        [Test]
        public void TestSerializeDeserializeArray()
        {
            bool[] boolVar = new bool[] { true, false, true, true, true, false };
            int[] intVar = new int[] { 22, -12, 12, 0, 2 };
            float[] floatVar = new float[] { 0.33f, 1f, 1.00001f, Mathf.Epsilon };
            string[] stringVar = new string[] { "cat", "dog", "frog", null, "me", "you" };
            byte[] byteVar = new byte[] { 2, 64, 100, 0, 2 };
            Vector2[] vector2Var = new Vector2[] { new Vector2(1, -2), new Vector2(0, 0), new Vector2(-0.5f, .44f) };
            Vector3[] vector3Var = new Vector3[] { new Vector3(1, 0, -2), new Vector3(0, 0, 0), new Vector3(0.33f, 0.33f, 0.66f) };
            Vector4[] vector4Var = new Vector4[] { new Vector4(1, -2, 0, 0.3f), new Vector4(0, 0, 0, 0), new Vector4(1, 0, 0, 0) };
            Quaternion[] quaternionVar = new Quaternion[] { Quaternion.Euler(90f, 30f, 15f), Quaternion.Euler(90f, 15f, 30f), Quaternion.Euler(0, 0, 0), Quaternion.Euler(90f, 0f, 14f) };
            Color[] colorVar = new Color[] { new Color(1, 2, 0, 0.33f), new Color(0, 3, 5, 0.33f), new Color(0, 0, 0, 0) };

            byte[] data = null;

            using (var serializationStream = new MemoryStream())
            using (var writer = new BinaryWriter(serializationStream))
            {
                Serialization.GetSerializer(typeof(bool)).Serialize(writer, boolVar, true);
                Serialization.GetSerializer(typeof(int)).Serialize(writer, intVar, true);
                Serialization.GetSerializer(typeof(float)).Serialize(writer, floatVar, true);
                Serialization.GetSerializer(typeof(string)).Serialize(writer, stringVar, true);
                Serialization.GetSerializer(typeof(byte)).Serialize(writer, byteVar, true);
                Serialization.GetSerializer(typeof(Vector2)).Serialize(writer, vector2Var, true);
                Serialization.GetSerializer(typeof(Vector3)).Serialize(writer, vector3Var, true);
                Serialization.GetSerializer(typeof(Vector4)).Serialize(writer, vector4Var, true);
                Serialization.GetSerializer(typeof(Quaternion)).Serialize(writer, quaternionVar, true);
                Serialization.GetSerializer(typeof(Color)).Serialize(writer, colorVar, true);
                data = serializationStream.ToArray();
            }

            using (var deserializationStream = new MemoryStream(data))
            using (var reader = new BinaryReader(deserializationStream))
            {
                Assert.AreEqual(boolVar, Serialization.GetSerializer(typeof(bool)).Deserialize(reader, true));
                Assert.AreEqual(intVar, Serialization.GetSerializer(typeof(int)).Deserialize(reader, true));
                Assert.AreEqual(floatVar, Serialization.GetSerializer(typeof(float)).Deserialize(reader, true));
                Assert.AreEqual(stringVar, Serialization.GetSerializer(typeof(string)).Deserialize(reader, true));
                Assert.AreEqual(byteVar, Serialization.GetSerializer(typeof(byte)).Deserialize(reader, true));
                Assert.AreEqual(vector2Var, Serialization.GetSerializer(typeof(Vector2)).Deserialize(reader, true));
                Assert.AreEqual(vector3Var, Serialization.GetSerializer(typeof(Vector3)).Deserialize(reader, true));
                Assert.AreEqual(vector4Var, Serialization.GetSerializer(typeof(Vector4)).Deserialize(reader, true));
                Assert.AreEqual(quaternionVar, Serialization.GetSerializer(typeof(Quaternion)).Deserialize(reader, true));
                Assert.AreEqual(colorVar, Serialization.GetSerializer(typeof(Color)).Deserialize(reader, true));
            }
        }
    }
}