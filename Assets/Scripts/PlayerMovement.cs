using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Hareket hızı")]
    public float moveSpeed = 5f;
    
    [Tooltip("Koşma hızı çarpanı")]
    public float runSpeedMultiplier = 1.5f;
    
    [Tooltip("Zıplama gücü")]
    public float jumpForce = 5f;
    
    [Tooltip("Yerçekimi çarpanı")]
    public float gravityMultiplier = 2.5f;
    
    [Header("Mouse Settings")]
    [Tooltip("Fare hassasiyeti")]
    public float mouseSensitivity = 2f;
    
    [Tooltip("Kamera Y ekseni maksimum açısı")]
    public float maxLookAngle = 80f;
    
    // Private variables
    private CharacterController controller;
    private Camera playerCamera;
    private float xRotation = 0f;
    private Vector3 velocity;
    private bool isGrounded;
    
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("PlayerMovement requires a CharacterController component!");
            this.enabled = false;
            return;
        }
        
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        // Fare imlecini kilitle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }
    
    private void HandleMouseLook()
    {
        // Fare girişini al
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // X ve Y ekseninde rotasyonu hesapla
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
        
        // Kamerayı sadece yukarı aşağı döndür (X rotasyonu)
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
        // Karakteri sadece sağa sola döndür (Y rotasyonu)
        transform.Rotate(Vector3.up * mouseX);
    }
    
    private void HandleMovement()
    {
        // Zeminde miyiz kontrol et
        isGrounded = controller.isGrounded;
        
        // Eğer zemindeyse, dikey hızı sıfırla
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Tam sıfır yerine küçük bir negatif değer daha iyi çalışır
        }
        
        // Hareket girişlerini al
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        
        // Hareket yönünü hesapla
        Vector3 move = transform.right * x + transform.forward * z;
        
        // Köşegen hareketi normalleştir
        if (move.magnitude > 1f)
        {
            move.Normalize();
        }
        
        // Koşma durumunda hızı artır
        float currentSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed *= runSpeedMultiplier;
        }
        
        // Karakteri hareket ettir
        controller.Move(move * currentSpeed * Time.deltaTime);
        
        // Zıplama
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
        }
        
        // Yerçekimi uygula
        velocity.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    
    // ESC tuşu ile fare kilidini aç/kapat
    private void OnGUI()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
} 