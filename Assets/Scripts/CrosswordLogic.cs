using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Crossword
{
    public class CrosswordLogic : MonoBehaviour
    {
        //VARIABILI
        private static CrosswordLogic instance;

        [SerializeField] private Tassello tasselloPrefab;
        [SerializeField] private GameObject crosswordParent;
        [SerializeField] private InputField inputField;
        [SerializeField] private Text pamparapiText;

        private List<WordObject> wordObjects = new List<WordObject>();
        private WordObject currentSelectedWordObject = null;
        private Tassello currentSelectedTassello = null;


        //PROPERTIES
        public static CrosswordLogic Instance => instance;
        [SerializeField] private Tassello TasselloPrefab => tasselloPrefab;
        [SerializeField] private GameObject CrosswordPanel => crosswordParent;
        public WordObject CurrentSelectedWordObject
        {
            set
            {
                if (currentSelectedWordObject != null)
                    if(!currentSelectedWordObject.Completed)
                        foreach (Tassello tassello in currentSelectedWordObject.tasselli)
                            SetTasselloColor(tassello, Color.white);

                currentSelectedWordObject = value;

                if(currentSelectedWordObject != null)
                    foreach (Tassello tassello in currentSelectedWordObject.tasselli)
                        SetTasselloColor(tassello, Color.cyan);

                //------debug
                if(currentSelectedWordObject != null)
                    pamparapiText.text = currentSelectedWordObject.GetWordInfo.word;
            }
            get { return currentSelectedWordObject; }
        }
        public Tassello CurrentSelectedTassello 
        {
            set
            {
                if (CurrentSelectedTassello)
                    if(CurrentSelectedTassello.wordObjectParents[0] == currentSelectedWordObject || CurrentSelectedTassello.wordObjectParents[1] == currentSelectedWordObject)
                        SetTasselloColor(currentSelectedTassello, Color.cyan);
                    else
                        SetTasselloColor(currentSelectedTassello, Color.white);
                currentSelectedTassello = value;
                if (CurrentSelectedTassello)
                    SetTasselloColor(currentSelectedTassello, Color.yellow);
            }
            get { return currentSelectedTassello; }
        }

        private bool LevelCompleted {
            get {
                foreach (WordObject wordObject in wordObjects)
                    if (!wordObject.Completed)
                        return false;
                return true;
            }
        }

        //METODI UNITY

        private void Awake()
        {
            InstantiateSelf();
        }

        void Start()
        {
            GenerateCrossword();
        }

        private void Update()
        {

        }

        #region METODI

        private void InstantiateSelf()
        {
            if (instance == null)
            {
                instance = this;
                //DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void GenerateCrossword()
        {
            ResetValuesAndGameBoard();
            GenerationInfo generationInfo = CrosswordGenerator.GenerateWords();

            WordInfo[] wordInfos = generationInfo.wordInfoList.ToArray();
            float tasselloSize = tasselloPrefab.GetComponent<RectTransform>().rect.width;
            Vector3 offSet = -new Vector3((generationInfo.xMax + generationInfo.xMin) / 2f, -(generationInfo.yMax + generationInfo.yMin) / 2f, 0) * tasselloSize; ;
            List<(int, int, Tassello)> insertions = new List<(int, int, Tassello)>();
            foreach (WordInfo wordInfo in wordInfos)
            {
                WordObject newWordobject = new WordObject(wordInfo);
                
                wordObjects.Add(newWordobject);
                AddTasselliToGameWord(newWordobject, tasselloSize, offSet, insertions);
            }
        }

        private void ResetValuesAndGameBoard()
        {
            CurrentSelectedWordObject = null;
            CurrentSelectedTassello = null;
            wordObjects.Clear();
            foreach (Transform child in crosswordParent.GetComponentInChildren<Transform>())
                Destroy(child.gameObject);

            pamparapiText.text = "Loser";
        }

        //NON MI PIACE COME CONTROLLA SE E' GIA STATO PIAZZATO UN TASSELLO, MA FUNZIONA COMUNQUE LOL
        private void AddTasselliToGameWord(WordObject wordObject, float tasselloSize, Vector3 offSet, List<(int, int, Tassello)> insertions = null)
        {
            for (int i = 0; i < wordObject.GetWordInfo.word.Length; i++)
            {
                Vector2Int coords;
                if(wordObject.GetWordInfo.horizontal)
                    coords = new Vector2Int(wordObject.GetWordInfo.x + i, wordObject.GetWordInfo.y);
                else
                    coords = new Vector2Int(wordObject.GetWordInfo.x, wordObject.GetWordInfo.y + i);

                Tassello tassello = null;
                //Controllo se un tassello l� era gi� posizionato. Se s� assegno a tassello quel valore
                foreach ((int, int, Tassello) triple in insertions)
                {
                    if (triple.Item1 == coords.x && triple.Item2 == coords.y)
                    {
                        tassello = triple.Item3;
                        break;
                    }
                }

                //Assegnazione valori nei 2 casi diversi
                if (tassello == null)
                {
                    tassello = GameObject.Instantiate(TasselloPrefab, CrosswordPanel.transform);
                    tassello.transform.localPosition += (new Vector3(coords.x,-coords.y,0) * tasselloSize) + offSet;
                    tassello.wordObjectParents[0] = wordObject;
                    insertions.Add((coords.x, coords.y, tassello));
                }
                else
                {
                    tassello.wordObjectParents[1] = wordObject;
                }
                wordObject.tasselli[i] = tassello;
            }
        }

        public void OnWordSelection(WordObject wordObject)
        {
           if (!wordObject.Completed)
            {
                CurrentSelectedWordObject = wordObject;
                CurrentSelectedTassello = FindNextFreeTassello(CurrentSelectedWordObject);
                if(CurrentSelectedTassello == null)
                    CurrentSelectedTassello = wordObject.tasselli[wordObject.tasselli.Length-1];
            }
        }

        private Tassello FindNextFreeTassello(WordObject wordObject)
        {
            foreach (Tassello tassello in wordObject.tasselli)
            {
                if (tassello.Lettera == ' ')
                    return tassello;
            }
            return null;
        }

        /// <summary>
        /// Called when a tassello is pressed
        /// </summary>
        /// <param name="lettera"></param>
        /*public void OnTasselloPress(char lettera)
        {
            if (CurrentSelectedTassello)
            {
                AddLetter(CurrentSelectedTassello, lettera);
                CurrentSelectedTassello = FindNextFreeTassello(CurrentSelectedWordObject);
            }
        }*/

        public void OnBackSpacePress()
        {
            RemoveLetter();
        }

        private void AddLetter(Tassello tassello, char lettera)
        {
            if (tassello)
            {
                tassello.SetLettera(lettera);
                foreach (WordObject wordObject in tassello.wordObjectParents)
                    if(wordObject != null) {
                        if (wordObject.CheckWordCompletion())
                        {
                            Debug.Log("Word was completed correctly");
                            foreach(Tassello tassello2 in wordObject.tasselli)
                                SetTasselloColor(tassello2, Color.green, false);
                            if (LevelCompleted)
                            {
                                Debug.Log("GAME HAS ENDED!!!!!");
                                GameCompletion();
                            }
                            else
                            {
                                CurrentSelectedWordObject = GetNotCompletedWord();
                                CurrentSelectedTassello = FindNextFreeTassello(CurrentSelectedWordObject);
                            }
                        }
                        else
                        {
                            //cambio solo se c'� un altro tassello libero (ovvero non era l'ultima lettera che ho inserito)
                            Tassello nextFreeTassello = FindNextFreeTassello(CurrentSelectedWordObject);
                            if(nextFreeTassello)
                                CurrentSelectedTassello = nextFreeTassello;
                            else
                            {
                                //Comportamento "hai riempito la parola ma � scorretta"
                                //METTI QUALCHE ANIMAZIONCINA!
                                CurrentSelectedTassello.SetLettera(' ');
                            }
                        }
                    }
            }
        }

        private void RemoveLetter()
        {
            bool selectionTasselloWasReached = false;
            for (int i = currentSelectedWordObject.tasselli.Length - 1; i >= 0; i--)
            {
                if (selectionTasselloWasReached)
                {
                    bool valid = true;
                    foreach (WordObject parent in currentSelectedWordObject.tasselli[i].wordObjectParents)
                        if (parent != null)
                            if (parent.Completed)
                                valid = false;
                    if (valid)
                    {
                        CurrentSelectedTassello = currentSelectedWordObject.tasselli[i];
                        CurrentSelectedTassello.SetLettera(' ');
                        break;
                    }
                }
                else if (currentSelectedWordObject.tasselli[i] == currentSelectedTassello)
                    selectionTasselloWasReached = true;
            }
        }

        public WordObject GetNotCompletedWord()
        {
            foreach(WordObject wordObject in wordObjects)
                if(!wordObject.Completed)
                    return wordObject;
            return null;
        }

        private void GameCompletion()
        {
            currentSelectedTassello = null;
            currentSelectedWordObject = null;
            pamparapiText.text = "GGWP";
        }

        public void CallLetterInsert(int asciiCode)
        {
            //gli acsii per le lettere maiuscole dalla A alla Z sono da 65 a 90
            AddLetter(currentSelectedTassello, (char)asciiCode);
        }

        public void CallLetterRemoval()
        {
            RemoveLetter();
        }

        private void SetTasselloColor(Tassello tassello, Color color, bool checkForParentCompletion = true)
        {
            if (checkForParentCompletion)
            {
                foreach (WordObject parent in tassello.wordObjectParents)
                    if (parent != null)
                        if (parent.Completed)
                            return;
            }
            
            tassello.GetComponent<Image>().color = color;
        }

        #endregion
    }

}
