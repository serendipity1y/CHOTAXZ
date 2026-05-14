using UnityEngine;

public class DoorAuto : MonoBehaviour
{
    public HingeJoint hinge;
    public float openSpeed = 200f;
    private bool canOpen;

    private JointMotor motor;

    void Start()
    {
        motor = hinge.motor;
        motor.force = 100;
    }

    public void Open()
    {
        if (!canOpen) return;
        Debug.Log("Opened");
        motor.targetVelocity = openSpeed;
        hinge.motor = motor;
        hinge.useMotor = true;
    }

    public void Close()
    {
        if (!canOpen) return;
        Debug.Log("Closed");
        motor.targetVelocity = -openSpeed;
        hinge.motor = motor;
        hinge.useMotor = true;
    }
}