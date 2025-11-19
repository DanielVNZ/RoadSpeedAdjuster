using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace RoadSpeedAdjuster.Components
{
    public struct CustomSpeed : IComponentData, IQueryTypeParameter, IEquatable<CustomSpeed>, ISerializable
    {
        public float m_Speed;

        public CustomSpeed(float value)
        {
            m_Speed = value;
        }

        public bool Equals(CustomSpeed other)
        {
            return Math.Abs(m_Speed - other.m_Speed) < 0.01f;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Speed);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Speed);
        }
    }
}