using UnityEngine;

namespace DreamHacks.Battle
{
    // Audio de la batalla: música de fondo en loop + efectos de sonido.
    // Carga los clips desde Resources/Audio en Awake para no depender de
    // wiring en la escena (igual que BattleUI/BattleManager se arman por código).
    public class BattleAudio : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float volumenMusica = 0.5f;
        [SerializeField, Range(0f, 1f)] private float volumenSfx = 0.85f;

        private AudioSource _musica;
        private AudioSource _sfx;
        private AudioClip _combateNormal, _combateHacker, _gameOver;
        private AudioClip _espada, _golpe, _glitch;

        private void Awake()
        {
            _musica = gameObject.AddComponent<AudioSource>();
            _musica.loop = true; _musica.playOnAwake = false; _musica.volume = volumenMusica;
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.loop = false; _sfx.playOnAwake = false; _sfx.volume = volumenSfx;

            _combateNormal = Resources.Load<AudioClip>("Audio/combate_normal");
            _combateHacker = Resources.Load<AudioClip>("Audio/combate_hacker");
            _gameOver = Resources.Load<AudioClip>("Audio/game_over");
            _espada = Resources.Load<AudioClip>("Audio/sfx_espada");
            _golpe = Resources.Load<AudioClip>("Audio/sfx_golpe");
            _glitch = Resources.Load<AudioClip>("Audio/sfx_glitch");
        }

        public void ReproducirMusica(bool hacker = false)
        {
            var c = hacker ? _combateHacker : _combateNormal;
            if (c == null || _musica == null) return;
            _musica.clip = c;
            _musica.loop = true;
            _musica.Play();
        }

        public void PararMusica() { if (_musica != null) _musica.Stop(); }

        public void Espada() => Sfx(_espada);
        public void Golpe() => Sfx(_golpe);
        public void Glitch() => Sfx(_glitch);

        public void Derrota()
        {
            PararMusica();
            Sfx(_gameOver);
        }

        private void Sfx(AudioClip c)
        {
            if (c != null && _sfx != null) _sfx.PlayOneShot(c);
        }
    }
}
