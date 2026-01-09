using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SistemaHacking
{
    public class ControlloreTerminale : MonoBehaviour
    {
        [Header("Componenti UI")]
        [SerializeField] private Text testoTerminale;
        [SerializeField] private InputField inputComando;
        [SerializeField] private ScrollRect scrollTerminale;
        [SerializeField] private int maxLineeTerminale = 50;

        [Header("Comandi")]
        [SerializeField] private string prompt = "C:\\HACK>";
        
        private GestoreHacking gestoreHacking;
        private SistemaComandi sistemaComandi;
        private List<string> storicoComandi = new List<string>();
        private int indiceStorico = 0;

        void Start()
        {
            gestoreHacking = FindObjectOfType<GestoreHacking>();
            sistemaComandi = new SistemaComandi(gestoreHacking);
            
            InizializzaTerminale();
        }

        void Update()
        {
            // Permette di inviare comandi con Enter
            if (inputComando.isFocused && Input.GetKeyDown(KeyCode.Return))
            {
                string comando = inputComando.text.Trim();
                if (!string.IsNullOrEmpty(comando))
                {
                    ElaboraComando(comando);
                }
                inputComando.text = "";
                inputComando.ActivateInputField();
            }

            // Navigazione storico con frecce
            if (inputComando.isFocused && Input.GetKeyDown(KeyCode.UpArrow))
            {
                MostraComandoPrecedente();
            }
            else if (inputComando.isFocused && Input.GetKeyDown(KeyCode.DownArrow))
            {
                MostraComandoSuccessivo();
            }
        }

        private void InizializzaTerminale()
        {
            AggiungiLineaTerminale("=== SISTEMA DI HACKING v2.0 ===");
            AggiungiLineaTerminale("Inizializzazione terminale... OK");
            AggiungiLineaTerminale("Caricamento moduli... OK");
            AggiungiLineaTerminale("");
            AggiungiLineaTerminale("Digita 'aiuto' per la lista dei comandi");
            AggiungiLineaTerminale("");
            AggiornaPrompt();
        }

        public void ElaboraComando(string comando)
        {
            if (string.IsNullOrWhiteSpace(comando))
                return;

            // Salva nel storico
            storicoComandi.Add(comando);
            indiceStorico = storicoComandi.Count;

            AggiungiLineaTerminale($"{prompt} {comando}");
            
            string risultato = sistemaComandi.EseguiComando(comando.ToLower());
            AggiungiLineaTerminale(risultato);
            
            AggiornaPrompt();
            AutoScroll();
        }

        private void MostraComandoPrecedente()
        {
            if (storicoComandi.Count == 0) return;

            indiceStorico = Mathf.Max(0, indiceStorico - 1);
            inputComando.text = storicoComandi[indiceStorico];
            inputComando.caretPosition = inputComando.text.Length;
        }

        private void MostraComandoSuccessivo()
        {
            if (storicoComandi.Count == 0) return;

            indiceStorico = Mathf.Min(storicoComandi.Count - 1, indiceStorico + 1);
            inputComando.text = storicoComandi[indiceStorico];
            inputComando.caretPosition = inputComando.text.Length;
        }

        private void AggiungiLineaTerminale(string testo)
        {
            testoTerminale.text += testo + "\n";
            
            // Limita il numero di linee per le performance
            string[] linee = testoTerminale.text.Split('\n');
            if (linee.Length > maxLineeTerminale)
            {
                testoTerminale.text = string.Join("\n", 
                    linee, linee.Length - maxLineeTerminale, maxLineeTerminale);
            }
        }

        private void AggiornaPrompt()
        {
            AggiungiLineaTerminale("");
            AggiungiLineaTerminale(prompt);
        }

        private void AutoScroll()
        {
            Canvas.ForceUpdateCanvases();
            scrollTerminale.verticalNormalizedPosition = 0f;
        }

        // Metodi pubblici per UI
        public void OnPulsanteInvia()
        {
            string comando = inputComando.text.Trim();
            if (!string.IsNullOrEmpty(comando))
            {
                ElaboraComando(comando);
                inputComando.text = "";
                inputComando.ActivateInputField();
            }
        }

        public void PulisciTerminale()
        {
            testoTerminale.text = "";
            InizializzaTerminale();
        }
    }
}

// Classe separata per gestire i comandi
public class SistemaComandi
{
    private GestoreHacking gestoreHacking;

    public SistemaComandi(GestoreHacking gestore)
    {
        gestoreHacking = gestore;
    }

    public string EseguiComando(string comando)
    {
        string[] parti = comando.Split(' ');
        string comandoBase = parti[0];

        switch (comandoBase)
        {
            case "aiuto":
                return MostraAiuto();
                
            case "avvia":
                return GestisciAvvia(parti);
                
            case "scansiona":
                return "Scansione rete in corso...\n" +
                       "Nodi rilevati: 5\n" +
                       "Sicurezza: MEDIA\n" +
                       "Firewall: ATTIVO";
                
            case "stato":
                return MostraStato();
                
            case "pulisci":
                return "Terminale pulito. Digita 'aiuto' per i comandi.";
                
            case "esci":
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                return "Terminale chiuso";
                
            case "info":
                return MostraInformazioniSistema();
                
            default:
                return $"Comando non riconosciuto: {comando}\nDigita 'aiuto' per la lista dei comandi";
        }
    }

    private string MostraAiuto()
    {
        return "=== COMANDI DISPONIBILI ===\n" +
               "aiuto - Mostra questo messaggio\n" +
               "avvia [facile|medio|difficile] - Avvia hacking\n" +
               "scansiona - Scansiona la rete\n" +
               "stato - Mostra stato sistema\n" +
               "pulisci - Pulisce lo schermo\n" +
               "info - Informazioni sul sistema\n" +
               "esci - Chiudi terminale";
    }

    private string MostraInformazioniSistema()
    {
        return "=== INFORMAZIONI SISTEMA ===\n" +
               "Versione: Hacking Terminal v2.0\n" +
               "Sviluppatore: BlackCamelot Studios\n" +
               "Linguaggio: C# / Unity\n" +
               "Tipo: Simulatore di hacking\n" +
               "Licenza: Proprietaria";
    }

    private string GestisciAvvia(string[] parti)
    {
        if (parti.Length < 2)
        {
            return "Specifica difficoltà: avvia [facile|medio|difficile]";
        }

        string difficolta = parti[1];
        
        if (gestoreHacking == null)
        {
            return "ERRORE: Sistema hacking non disponibile";
        }
        
        return difficolta switch
        {
            "facile" => AvviaGiocoConDifficolta(Difficolta.Facile),
            "medio" => AvviaGiocoConDifficolta(Difficolta.Medio),
            "difficile" => AvviaGiocoConDifficolta(Difficolta.Difficile),
            _ => "Difficoltà non valida. Usa: facile, medio, difficile"
        };
    }

    private string AvviaGiocoConDifficolta(Difficolta diff)
    {
        if (gestoreHacking != null)
        {
            gestoreHacking.AvviaGioco(diff);
            return $"Sessione hacking avviata - Difficoltà: {diff}\n" +
                   $"Collega il nodo obiettivo per violare il sistema!\n" +
                   $"Usa il mouse per cliccare sui nodi di rete.";
        }
        
        return "ERRORE: Sistema hacking non disponibile";
    }

    private string MostraStato()
    {
        if (gestoreHacking == null)
            return "Sistema non disponibile";
        
        return $"=== STATO SISTEMA ===\n" +
               $"Gioco attivo: {(gestoreHacking.IsGiocoAttivo() ? "SI" : "NO")}\n" +
               $"Tentativi rimasti: {gestoreHacking.GetTentativiRimanenti()}\n" +
               $"Tempo rimasto: {gestoreHacking.GetTempoRimanente():F1}s";
    }
}