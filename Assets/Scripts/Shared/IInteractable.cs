using UnityEngine;

public interface IInteractable
{
    Transform Transform { get; }

    string DisplayName { get; }

    float HoldDuration { get; }

    bool IsInRange(Transform interactor);

    void SetHighlighted(bool highlighted);

    void BeginHold(GameObject interactor);
    void CancelHold(GameObject interactor);
}
