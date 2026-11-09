using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace YYOU.StartScreen
{
    public sealed class StartScreenView : MonoBehaviour
    {
        public UnityEvent onStartRequested = new UnityEvent();
        public Button startButton;
        public GameObject settingsPanel;
        public Slider volumeSlider;
        public GameObject songPacksPanel;
        public GameObject songSelectionPrefab;
        public GameObject songPackShopPrefab;
        public YYOU.Settings.KeyBindingSettings keyBindingSettings;
        YYOU.SongSelection.SongSelectionView songSelection;
        YYOU.SongPacks.SongPackShopView songPackShop;
        void Start()
        {
            Application.targetFrameRate=60;
            volumeSlider.SetValueWithoutNotify(YYOU.Settings.PlayerAudioSettings.Music);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(startButton.gameObject);
        }
        void Update()
        {
            bool capturing=keyBindingSettings && (keyBindingSettings.IsCapturing || keyBindingSettings.ConsumedFrame==Time.frameCount);
            if (Input.GetKeyDown(KeyCode.Escape) && settingsPanel.activeSelf && !capturing) CloseSettings();
            else if(Input.GetKeyDown(KeyCode.Escape) && songPacksPanel && songPacksPanel.activeSelf) CloseSongPacks();
        }
        public void RequestStart()
        {
            onStartRequested.Invoke();
            if(songSelectionPrefab){if(!songSelection)songSelection=Instantiate(songSelectionPrefab).GetComponent<YYOU.SongSelection.SongSelectionView>();songSelection.Open(gameObject);}
        }
        public void OpenSettings()
        {
            settingsPanel.SetActive(true);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(volumeSlider.gameObject);
        }
        public void CloseSettings()
        {
            settingsPanel.SetActive(false);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(startButton.gameObject);
        }
        public void SetVolume(float value) { YYOU.Settings.PlayerAudioSettings.SetMusic(value); }
        public void OpenSongPacks()
        {
            if(songPackShopPrefab){if(!songPackShop)songPackShop=Instantiate(songPackShopPrefab).GetComponent<YYOU.SongPacks.SongPackShopView>();songPackShop.Open(gameObject);return;}
            if(songPacksPanel){songPacksPanel.SetActive(true);if(EventSystem.current)EventSystem.current.SetSelectedGameObject(songPacksPanel.GetComponentInChildren<Button>().gameObject);}
        }
        public void CloseSongPacks(){if(songPackShop && songPackShop.gameObject.activeSelf)songPackShop.Close();if(songPacksPanel)songPacksPanel.SetActive(false);if(EventSystem.current)EventSystem.current.SetSelectedGameObject(startButton.gameObject);}
        public void OpenSong(string id){RequestStart();if(songSelection)songSelection.FocusSong(id);}
        public void Quit()
        {
            #if UNITY_EDITOR
            Debug.Log("YYOU: 退出功能在打包后生效。");
            #else
            Application.Quit();
            #endif
        }
    }
}
