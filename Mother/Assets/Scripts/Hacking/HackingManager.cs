using UnityEngine;
using System.Collections.Generic;

namespace SistemaHacking
{
    public enum Difficolta
    {
        Facile,
        Medio,
        Difficile
    }

    public class GestoreHacking : MonoBehaviour
    {
        [Header("Configurazione Gioco")]
        [SerializeField] private GameObject prefabNodo;
        [SerializeField] private Transform contenitoreNodi;
        [SerializeField] private int tentativiMassimi = 3;
        [SerializeField] private float tempoLimite = 60f;

        [Header("Configurazione Difficoltà")]
        [SerializeField] private ConfigurazioneDifficolta configFacile;
        [SerializeField] private ConfigurazioneDifficolta configMedio;
        [SerializeField] private ConfigurazioneDifficolta configDifficile;

        private Difficolta difficoltaCorrente;
        private int tentativiRimanenti;
        private float tempoRimanente;
        private bool giocoAttivo = false;
        private List<NodoRete> nodiGenerati = new List<NodoRete>();
        private NodoRete nodoObiettivo;

        [System.Serializable]
        public class ConfigurazioneDifficolta
        {
            public int numeroNodiMin = 3;
            public int numeroNodiMax = 5;
            public int connessioniMassime = 2;
            public float complessita = 0.3f;
        }

        void Start()
        {
            InizializzaSistema();
        }

        void Update()
        {
            if (giocoAttivo)
            {
                AggiornaTimer();
            }
        }

        private void InizializzaSistema()
        {
            // Resetta tutte le variabili del gioco
            tentativiRimanenti = tentativiMassimi;
            tempoRimanente = tempoLimite;
        }

        public void AvviaGioco(Difficolta difficolta)
        {
            difficoltaCorrente = difficolta;
            giocoAttivo = true;
            
            // Pulisci nodi esistenti
            PulisciNodi();
            
            // Genera nuova rete
            GeneratoreNodi generatore = new GeneratoreNodi();
            ConfigurazioneDifficolta config = OttieniConfigurazione(difficolta);
            nodiGenerati = generatore.GeneraRete(config, prefabNodo, contenitoreNodi);
            
            // Seleziona nodo obiettivo casuale
            SelezionaNodoObiettivo();
            
            Debug.Log($"Gioco hacking avviato - Difficoltà: {difficolta}");
            Debug.Log($"Nodi generati: {nodiGenerati.Count}");
            Debug.Log($"Nodo obiettivo: {nodoObiettivo.ID}");
        }

        private ConfigurazioneDifficolta OttieniConfigurazione(Difficolta diff)
        {
            return diff switch
            {
                Difficolta.Facile => configFacile,
                Difficolta.Medio => configMedio,
                Difficolta.Difficile => configDifficile,
                _ => configFacile
            };
        }

        private void SelezionaNodoObiettivo()
        {
            if (nodiGenerati.Count > 0)
            {
                int indiceCasuale = Random.Range(0, nodiGenerati.Count);
                nodoObiettivo = nodiGenerati[indiceCasuale];
                nodoObiettivo.ImpostaComeObiettivo(true);
            }
        }

        public void ProvaAccesso(NodoRete nodo)
        {
            if (!giocoAttivo) return;

            tentativiRimanenti--;
            
            if (nodo == nodoObiettivo)
            {
                Vittoria();
            }
            else
            {
                Debug.Log($"Accesso fallito! Tentativi rimanenti: {tentativiRimanenti}");
                
                if (tentativiRimanenti <= 0)
                {
                    Sconfitta();
                }
            }
        }

        private void AggiornaTimer()
        {
            tempoRimanente -= Time.deltaTime;
            
            if (tempoRimanente <= 0)
            {
                tempoRimanente = 0;
                Sconfitta();
            }
        }

        private void Vittoria()
        {
            giocoAttivo = false;
            Debug.Log("ACCESSO RIUSCITO! Sistema violato con successo.");
            // Qui potresti attivare effetti, suoni, ecc.
        }

        private void Sconfitta()
        {
            giocoAttivo = false;
            Debug.Log("ACCESSO NEGATO! Sistema di sicurezza attivato.");
            // Qui potresti attivare effetti, suoni, ecc.
        }

        private void PulisciNodi()
        {
            foreach (Transform child in contenitoreNodi)
            {
                Destroy(child.gameObject);
            }
            nodiGenerati.Clear();
        }

        // Metodi pubblici per l'UI
        public int GetTentativiRimanenti() => tentativiRimanenti;
        public float GetTempoRimanente() => tempoRimanente;
        public bool IsGiocoAttivo() => giocoAttivo;
    }
}