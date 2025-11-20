using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace RoadSpeedAdjuster.Components
{
    public struct CustomSpeed : IComponentData, IQueryTypeParameter, IEquatable<CustomSpeed>, ISerializable
    {
        public float m_Speed;      // Speed in KPH
        public float m_SpeedMPH;   // Speed in MPH (for NA lane markings)

        public CustomSpeed(float speedKPH)
        {
            m_Speed = speedKPH;
            m_SpeedMPH = speedKPH * 0.621371f; // Convert KPH to MPH
        }

        public bool Equals(CustomSpeed other)
        {
            return Math.Abs(m_Speed - other.m_Speed) < 0.01f;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(m_Speed);
            writer.Write(m_SpeedMPH);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out m_Speed);
            reader.Read(out m_SpeedMPH);
        }
    }
}