using UnityEngine;

// ===== Bloqueo1..2 =====
// Pared invisible hacia el cuarto principal. Se abre cuando se vieron los 8 diálogos.
public class BloqueoHospital : ZonaInvisible
{
    [TextArea] public string mensajeBloqueado = "(Un campo de estática cierra el paso. Faltan recuerdos por encontrar.)";
    float avisoHasta;

    protected override void Awake() { base.Awake(); col.isTrigger = false; }

    void Start()
    {
        var p = ProgresoHospital.I;
        if (p.Completo) Abrir(); else p.AlCompletar += Abrir;
    }

    void Abrir() { col.enabled = false; }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (!EsPlayer(c.collider) || DialogoUI.Activo || Time.time < avisoHasta) return;
        avisoHasta = Time.time + 3f;
        int faltan = ProgresoHospital.I.totalRequeridos - ProgresoHospital.I.Vistos;
        DialogoUI.I.MostrarUna("Narrador", mensajeBloqueado + $" ({faltan} restantes)");
    }
}
