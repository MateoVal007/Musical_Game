using UnityEngine;
using UnityEngine.InputSystem;
using System;

// Detecta un golpe de muñeca con el control derecho — el gesto de tocar
// batería, no un swing de todo el brazo — y en qué dirección fue (arriba,
// abajo, izquierda, derecha), relativa a hacia dónde mira el jugador.
// Sigue hacia dónde apunta el control (transform.forward) cuadro a cuadro;
// cuando esa dirección se mueve rápido, cuenta como golpe.
//
// El gatillo actúa como "armado": el movimiento solo cuenta mientras está
// apretado (apretar, hacer el golpe, soltar) — así cualquier gesto de la
// muñeca que no sea intencional (acomodarse, rascarse, etc.) no cuenta como
// nota muerta.
//
// Umbral de doble golpe (histéresis): una vez que se dispara un golpe, la
// velocidad tiene que bajar de resetSpeedThreshold antes de permitir el
// próximo — si no, un solo golpe podría contar 2-3 veces.
public class DrumHitDetector : MonoBehaviour
{
    [Tooltip("Transform del controlador derecho (el que gira con la muñeca en VR, o con el simulador en el Editor).")]
    [SerializeField] private Transform handTransform;

    [Tooltip("La acción 'Activate' del control (ej. XRI Right/Activate — el gatillo). Mientras no esté apretada, se ignora el movimiento de la muñeca.")]
    [SerializeField] private InputActionReference triggerAction;

    [Tooltip("Qué tan rápido tiene que moverse la dirección a la que apunta el control para contar como golpe.")]
    [SerializeField] private float hitSpeedThreshold = 1.0f;

    [Tooltip("La velocidad tiene que caer por debajo de esto antes de permitir el próximo golpe.")]
    [SerializeField] private float resetSpeedThreshold = 0.5f;

    [Tooltip("Segundos de espera después de cada golpe antes de poder detectar el próximo — evita que el rebote de vuelta de la muñeca (después de golpear) se cuente como un segundo golpe en la dirección contraria.")]
    [SerializeField] private float hitCooldown = 0.45f;

    [Tooltip("Activalo para ver en la consola la velocidad y dirección detectadas cuadro a cuadro — útil para calibrar. Apagalo después, tira un log por frame.")]
    [SerializeField] private bool debugLogVelocity = false;

    public event Action<NoteDirection> OnDirectionalHit;

    private Vector3 lastForward;
    private bool readyForHit = true;
    private float lastHitTime = -999f;
    private Camera playerCamera;

    void OnEnable()
    {
        if (triggerAction != null) triggerAction.action.Enable();
    }

    void OnDisable()
    {
        if (triggerAction != null) triggerAction.action.Disable();
    }

    void Start()
    {
        lastForward = handTransform.forward;
        playerCamera = Camera.main;
    }

    void Update()
    {
        Vector3 currentForward = handTransform.forward;
        Vector3 delta = currentForward - lastForward;
        lastForward = currentForward;

        // Sin acción de gatillo asignada, se comporta como antes (siempre activo).
        bool triggerHeld = triggerAction == null || triggerAction.action.IsPressed();

        float speed = delta.magnitude / Time.deltaTime;

        if (debugLogVelocity) Debug.Log($"speed: {speed:F2} (umbral: {hitSpeedThreshold}) | gatillo: {triggerHeld}");

        bool cooldownElapsed = Time.time - lastHitTime >= hitCooldown;

        if (triggerHeld && readyForHit && cooldownElapsed && speed >= hitSpeedThreshold)
        {
            readyForHit = false;
            lastHitTime = Time.time;
            NoteDirection direction = ClassifyDirection(delta);
            if (debugLogVelocity) Debug.Log($"Golpe detectado: {direction}");
            OnDirectionalHit?.Invoke(direction);
        }
        else if (!readyForHit && speed < resetSpeedThreshold)
        {
            readyForHit = true;
        }
    }

    // Proyecta el movimiento sobre los ejes derecha/arriba de la cámara del
    // jugador (no los ejes del mundo) para que "izquierda/derecha/arriba/
    // abajo" siempre sea relativo a hacia dónde está mirando, no al mundo.
    private NoteDirection ClassifyDirection(Vector3 delta)
    {
        Transform reference = playerCamera != null ? playerCamera.transform : handTransform;

        float rightAmount = Vector3.Dot(delta, reference.right);
        float upAmount = Vector3.Dot(delta, reference.up);

        if (Mathf.Abs(upAmount) > Mathf.Abs(rightAmount))
        {
            return upAmount < 0f ? NoteDirection.Down : NoteDirection.Up;
        }

        return rightAmount < 0f ? NoteDirection.Left : NoteDirection.Right;
    }
}
