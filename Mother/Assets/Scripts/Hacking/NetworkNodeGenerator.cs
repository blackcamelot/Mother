using UnityEngine;
using System.Collections.Generic;

namespace SistemaHacking
{
    public class GeneratoreNodi
    {
        public List<NodoRete> GeneraRete(
            GestoreHacking.ConfigurazioneDifficolta configurazione,
            GameObject prefabNodo,
            Transform contenitore)
        {
            List<NodoRete> nodi = new List<NodoRete>();
            
            // Determina numero di nodi
            int numeroNodi = Random.Range(
                configurazione.numeroNodiMin,
                configurazione.numeroNodiMax + 1
            );
            
            // Crea nodi
            for (int i = 0; i < numeroNodi; i++)
            {
                Vector2 posizione = CalcolaPosizioneNodo(i, numeroNodi);
                NodoRete nodo = CreaNodo(prefabNodo, contenitore, posizione, $"NODO_{i:00}");
                nodi.Add(nodo);
            }
            
            // Crea connessioni
            GeneraConnessioni(nodi, configurazione.connessioniMassime);
            
            return nodi;
        }

        private Vector2 CalcolaPosizioneNodo(int indice, int totaleNodi)
        {
            // Dispone i nodi in cerchio
            float angolo = (indice * 360f / totaleNodi) * Mathf.Deg2Rad;
            float raggio = 200f; // Raggio in pixel/unità
            
            return new Vector2(
                Mathf.Cos(angolo) * raggio,
                Mathf.Sin(angolo) * raggio
            );
        }

        private NodoRete CreaNodo(GameObject prefab, Transform contenitore, Vector2 posizione, string id)
        {
            GameObject oggettoNodo = Object.Instantiate(prefab, contenitore);
            oggettoNodo.transform.localPosition = posizione;
            
            NodoRete nodo = oggettoNodo.GetComponent<NodoRete>();
            if (nodo != null)
            {
                nodo.Inizializza(id);
            }
            
            return nodo;
        }

        private void GeneraConnessioni(List<NodoRete> nodi, int connessioniMassime)
        {
            foreach (var nodo in nodi)
            {
                // Determina numero di connessioni per questo nodo
                int numConnessioni = Random.Range(1, connessioniMassime + 1);
                
                // Assicura che ogni nodo abbia almeno una connessione
                if (OttieniConnessioniAttuali(nodo) == 0)
                {
                    numConnessioni = Mathf.Max(1, numConnessioni);
                }
                
                // Crea connessioni
                for (int i = 0; i < numConnessioni; i++)
                {
                    NodoRete possibileNodo = TrovaNodoConnessioneValido(nodo, nodi);
                    if (possibileNodo != null)
                    {
                        nodo.AggiungiConnessione(possibileNodo);
                        possibileNodo.AggiungiConnessione(nodo); // Connessione bidirezionale
                    }
                }
            }
            
            // Verifica che tutti i nodi siano connessi
            AssicuraReteConnessa(nodi);
        }

        private int OttieniConnessioniAttuali(NodoRete nodo)
        {
            // Questo sarebbe implementato se NodoRete avesse un metodo per ottenere le connessioni
            // Per ora, restituiamo un valore casuale
            return Random.Range(0, 2);
        }

        private NodoRete TrovaNodoConnessioneValido(NodoRete nodoOrigine, List<NodoRete> tuttiNodi)
        {
            List<NodoRete> nodiDisponibili = new List<NodoRete>(tuttiNodi);
            nodiDisponibili.Remove(nodoOrigine);
            
            // Rimuovi nodi già connessi
            // Qui dovresti avere un modo per verificare le connessioni esistenti
            
            if (nodiDisponibili.Count > 0)
            {
                return nodiDisponibili[Random.Range(0, nodiDisponibili.Count)];
            }
            
            return null;
        }

        private void AssicuraReteConnessa(List<NodoRete> nodi)
        {
            if (nodi.Count == 0) return;
            
            // Algoritmo semplice per assicurarsi che tutti i nodi siano raggiungibili
            // (implementazione base - in un sistema reale useresti BFS o DFS)
            for (int i = 1; i < nodi.Count; i++)
            {
                // Collega ogni nodo al precedente per garantire la connessione
                nodi[i].AggiungiConnessione(nodi[i-1]);
                nodi[i-1].AggiungiConnessione(nodi[i]);
            }
        }
    }
}