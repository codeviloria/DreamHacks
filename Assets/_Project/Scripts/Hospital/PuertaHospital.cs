using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


// ===== Puerta1..3 =====
// Antes de completar: te teletransporta a OTRA puerta al azar y aparece un mensaje anónimo.
// Después de completar: cualquier puerta lleva a la escena final.
public class PuertaHospital : ZonaInvisible
{
    static readonly List<PuertaHospital> todas = new List<PuertaHospital>();
    static float bloqueoGlobalHasta;

    public string escenaFinal = "Final";
    [Tooltip("Dónde aparece el player al llegar a esta puerta (offset en unidades de mundo). Ajusta para que quede sobre el piso.")]
    public Vector2 offsetLlegada = new Vector2(0, -1.5f);

    static readonly string[] mensajes =
    {
        "Aún no has descubierto el camino real.",
        "ACCESO DENEGADO. Esta puerta no lleva a ningún lado... todavía.",
        "Das vueltas como un paquete perdido en la red.",
        "Error 404: salida no encontrada. Sigue buscando, rebelde.",
        "Cada puerta que abres es una que yo ya cerré.",
    };

    protected override void Awake() { base.Awake(); col.isTrigger = true; }
    void OnEnable() { todas.Add(this); }
    void OnDisable() { todas.Remove(this); }

    void OnTriggerEnter2D(Collider2D c)
    {
        if (!EsPlayer(c) || DialogoUI.Activo || Time.time < bloqueoGlobalHasta) return;
        bloqueoGlobalHasta = Time.time + 1f;

        if (ProgresoHospital.I && ProgresoHospital.I.Completo) { SceneManager.LoadScene(escenaFinal); return; }

        var otras = todas.FindAll(p => p != this);
        if (otras.Count == 0) return;
        var destino = otras[Random.Range(0, otras.Count)];
        var cuerpo = c.attachedRigidbody ? c.attachedRigidbody.transform : c.transform;
        Vector3 llegada = destino.transform.position + (Vector3)destino.offsetLlegada;
        if (c.attachedRigidbody) { c.attachedRigidbody.position = llegada; c.attachedRigidbody.linearVelocity = Vector2.zero; }
        cuerpo.position = llegada;
        DialogoUI.I.MostrarUna("Hacker", mensajes[Random.Range(0, mensajes.Length)]);
    }
}
