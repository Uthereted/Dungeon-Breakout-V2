using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Player.Movement
{
    public class Movement : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public Rigidbody2D rb;
        private Vector2 movement;

        // Update is called once per frame
        void Update()
        {
            // Get input from keyboard
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
        }

        void FixedUpdate()
        {
            // Move the character
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        }
    }


}
