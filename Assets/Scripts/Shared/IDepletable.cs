using UnityEngine.Events;

public interface IDepletable
{
    float Current { get; }
    float Max { get; }
    UnityEvent<float, float> Changed { get; }
}
