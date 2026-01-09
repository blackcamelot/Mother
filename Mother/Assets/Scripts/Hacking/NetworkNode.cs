using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace SistemaHacking
{
    public class NodoRete : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Componenti Nodo")]
        [SerializeField] private Image immagineNodo;
        [SerializeField] private Text testoID;
        [SerializeField] private LineRenderer prefabLinea;
        
        [Header("Colori Stato")]
        [SerializeField] private Color coloreNormale = Color.blue;
        [SerializeField] private Color coloreObiettivo = Color.green;
        [SerializeField] private Color coloreSelezionato = Color.yellow;
        [SerializeField] private Color coloreViolato = Color.red;

        private string idNodo;
        private bool isObiettivo = false;
        private bool isViolato = false;
        private List<NodoRete> connessioni = new List<NodoRete>();
        private List<LineRenderer> lineeConnessione = new List<LineRenderer>();
        private GestoreHacking gestoreHacking;

        public string ID => idNodo;

        void Start()
        {
            gestoreHacking = FindObjectOfType<GestoreHacking>();
            AggiornaAspetto();
        }

        public void Inizializza(string id, bool obiettivo = false)
        {
            idNodo = id;
            isObiettivo = obiettivo;
            testoID.text = id;
            AggiornaAspetto();
        }

        public void AggiungiConnessione(NodoRete altroNodo)
        {
            if (!connessioni.Contains(altroNodo))
            {
                connessioni.Add(altroNodo);
                CreaLineaConnessione(altroNodo);
            }
        }

        private void CreaLineaConnessione(NodoRete target)
        {
            if (prefabLinea == null) return;

            LineRenderer linea = Instantiate(prefabLinea, transform);
            linea.positionCount = 2;
            linea.SetPosition(0, transform.position);
            linea.SetPosition(1, target.transform.position);
            
            lineeConnessione.Add(linea);
        }

        public void ImpostaComeObiettivo(bool obiettivo)
        {
            isObiettivo = obiettivo;
            AggiornaAspetto();
        }

        public void MarcaComeViolato()
        {
            isViolato = true;
            AggiornaAspetto();
        }

        private void AggiornaAspetto()
        {
            if (immagineNodo == null) return;

            if (isViolato)
            {
                immagineNodo.color = coloreViolato;
            }
            else if (isObiettivo)
            {
                immagineNodo.color = coloreObiettivo;
            }
            else
            {
                immagineNodo.color = coloreNormale;
            }
        }

        // Interazione con il mouse
        public void OnPointerClick(PointerEventData eventData)
        {
            if (gestoreHacking != null && gestoreHacking.IsGiocoAttivo())
            {
                Debug.Log($"Tentativo di accesso al nodo: {idNodo}");
                gestoreHacking.ProvaAccesso(this);
                
                if (!isObiettivo)
                {
                    MarcaComeViolato();
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isViolato && !isObiettivo)
            {
                immagineNodo.color = coloreSelezionato;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            AggiornaAspetto();
        }

        void OnDestroy()
        {
            // Pulisci le linee di connessione
            foreach (var linea in lineeConnessione)
            {
                if (linea != null)
                    Destroy(linea.gameObject);
            }
        }
    }
}