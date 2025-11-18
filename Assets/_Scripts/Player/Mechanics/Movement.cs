using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    private Rigidbody2D _rb;
    [SerializeField] public float speed = 5f;
    
    private Vector2 _moveInput; // to store magnitudes for 2D movement

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }
    
    public void SetMoveInput(Vector2 input) => _moveInput = input; // expose this function for movement inputs in InputHandler
    
    private void FixedUpdate() // FixedUpdate for physics
    {
        var movement = _moveInput.normalized * speed; // basic movement with multiplying magnitude by speed
        _rb.linearVelocity = movement; // set this to player velocity
    }
}