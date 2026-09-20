using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

// El XR Interaction Simulator es persistente entre escenas, pero resuelve sus
// referencias a los controles SOLO en su propio OnEnable(), que corre una vez
// (en la primera escena) y nunca más. Cuando esa escena se destruye y aparece
// un XR Origin nuevo (otra escena), el Simulator queda con referencias rotas
// ("Missing") a los controles viejos -- el mouse le sigue llegando bien, pero
// no tiene a qué aplicárselo. Este script fuerza el reenganche al cargar cada
// escena nueva, con los controles DE ESA escena.
public class XRSimulatorRelinker : MonoBehaviour
{
    [SerializeField] private Transform leftControllerTransform;
    [SerializeField] private Transform rightControllerTransform;

    void Start()
    {
        var simulator = FindFirstObjectByType<XRInteractionSimulator>();
        if (simulator == null)
        {
            Debug.LogWarning("XRSimulatorRelinker: no encontré ningún XR Interaction Simulator en la escena.");
            return;
        }

        if (leftControllerTransform != null) simulator.leftControllerTransform = leftControllerTransform;
        if (rightControllerTransform != null) simulator.rightControllerTransform = rightControllerTransform;
    }
}