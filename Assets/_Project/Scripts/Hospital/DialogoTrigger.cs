using UnityEngine;

// ===== Dialogo1..8 =====
// Al tocarlo reproduce el SIGUIENTE diálogo pendiente (orden cronológico) y se desactiva.
public class DialogoTrigger : ZonaInvisible
{
    protected override void Awake() { base.Awake(); col.isTrigger = true; }

    void OnTriggerEnter2D(Collider2D c)
    {
        if (!EsPlayer(c) || DialogoUI.Activo) return;
        var p = ProgresoHospital.I; var d = p ? p.SiguientePendiente() : null;
        if (d == null) { gameObject.SetActive(false); return; }
        col.enabled = false;
        DialogoUI.I.Mostrar(d.lineas, () => { p.MarcarVisto(d); gameObject.SetActive(false); });
    }
}
