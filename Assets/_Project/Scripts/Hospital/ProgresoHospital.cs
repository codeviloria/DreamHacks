using System;
using System.Collections.Generic;
using UnityEngine;

// Lleva la cuenta de los diálogos de lore vistos en el Hospital.
// Los diálogos SIEMPRE salen en orden cronológico: el trigger que toques reproduce el siguiente pendiente.
public class ProgresoHospital : MonoBehaviour
{
    public static ProgresoHospital I { get; private set; }

    // "Disable Domain Reload": limpiar el singleton al entrar en Play para no
    // apuntar a una instancia destruida de la sesión anterior.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetEstaticos() { I = null; }

    [Tooltip("dialogos_hospital.json (TextAsset)")]
    public TextAsset jsonDialogos;
    public int totalRequeridos = 8;

    public DatosDialogos Datos { get; private set; }
    public int Vistos { get; private set; }
    public bool Completo => Vistos >= totalRequeridos;

    public event Action AlCompletar;
    public event Action<int> AlVerDialogo; // id visto

    void Awake()
    {
        I = this;
        Datos = JsonUtility.FromJson<DatosDialogos>(jsonDialogos.text);
        Datos.dialogos.Sort((a, b) => a.id.CompareTo(b.id));
    }

    /// <summary>Devuelve el siguiente diálogo pendiente (cronológico) o null si ya no quedan.</summary>
    public Dialogo SiguientePendiente() => Vistos < Datos.dialogos.Count ? Datos.dialogos[Vistos] : null;

    public void MarcarVisto(Dialogo d)
    {
        if (d == null || Completo) return;
        Vistos++;
        AlVerDialogo?.Invoke(d.id);
        if (Completo) AlCompletar?.Invoke();
    }

#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
    // Debug solo en el Editor: F8 marca todos los diálogos como vistos para
    // probar rápido bloqueos y puertas sin recorrer los 8 triggers.
    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.f8Key.wasPressedThisFrame)
        {
            while (!Completo) MarcarVisto(SiguientePendiente());
            Debug.Log("[Hospital] DEBUG F8: todos los diálogos marcados como vistos (" + Vistos + "/" + totalRequeridos + ").");
        }
    }
#endif
}

[Serializable] public class DatosDialogos { public List<Dialogo> dialogos = new List<Dialogo>(); }
[Serializable] public class Dialogo { public int id; public string titulo; public List<LineaDialogo> lineas = new List<LineaDialogo>(); }
[Serializable] public class LineaDialogo { public string quien; public string texto; }
