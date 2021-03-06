//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace UI
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;
    using BJSYGameCore.UI;
    
    public partial class MainMenu : UIObject
    {
        protected override void Awake()
        {
            base.Awake();
            this.autoBind();
            this.onAwake();
        }
        public void autoBind()
        {
            this._BackgroundImage = this.transform.Find("BackgroundContainer").Find("Background").GetComponent<Image>();
            this._ButtonPanelImage = this.transform.Find("ButtonPanel").GetComponent<Image>();
            this._ButtonsVerticalLayoutGroup = this.transform.Find("ButtonPanel").Find("Buttons").GetComponent<VerticalLayoutGroup>();
            this._ManMachineButton = this.transform.Find("ButtonPanel").Find("Buttons").Find("ManMachineButton").GetComponent<Button>();
            this._NetworkButton = this.transform.Find("ButtonPanel").Find("Buttons").Find("NetworkButton").GetComponent<Button>();
            this._BuildButton = this.transform.Find("ButtonPanel").Find("Buttons").Find("BuildButton").GetComponent<Button>();
            this._QuitButton = this.transform.Find("ButtonPanel").Find("Buttons").Find("QuitButton").GetComponent<Button>();
            this._LogoImage = this.transform.Find("Logo").GetComponent<Image>();
        }
        private Main _parent;
        public Main parent
        {
            get
            {
                return this.transform.parent.GetComponent<Main>();
            }
        }
        [SerializeField()]
        private Image _BackgroundImage;
        public Image BackgroundImage
        {
            get
            {
                if ((this._BackgroundImage == null))
                {
                    this._BackgroundImage = this.transform.Find("BackgroundContainer").Find("Background").GetComponent<Image>();
                }
                return this._BackgroundImage;
            }
        }
        [SerializeField()]
        private Image _ButtonPanelImage;
        public Image ButtonPanelImage
        {
            get
            {
                if ((this._ButtonPanelImage == null))
                {
                    this._ButtonPanelImage = this.transform.Find("ButtonPanel").GetComponent<Image>();
                }
                return this._ButtonPanelImage;
            }
        }
        [SerializeField()]
        private VerticalLayoutGroup _ButtonsVerticalLayoutGroup;
        public VerticalLayoutGroup ButtonsVerticalLayoutGroup
        {
            get
            {
                if ((this._ButtonsVerticalLayoutGroup == null))
                {
                    this._ButtonsVerticalLayoutGroup = this.transform.Find("ButtonPanel").Find("Buttons").GetComponent<VerticalLayoutGroup>();
                }
                return this._ButtonsVerticalLayoutGroup;
            }
        }
        [SerializeField()]
        private Button _ManMachineButton;
        public Button ManMachineButton
        {
            get
            {
                if ((this._ManMachineButton == null))
                {
                    this._ManMachineButton = this.transform.Find("ButtonPanel").Find("Buttons").Find("ManMachineButton").GetComponent<Button>();
                }
                return this._ManMachineButton;
            }
        }
        [SerializeField()]
        private Button _NetworkButton;
        public Button NetworkButton
        {
            get
            {
                if ((this._NetworkButton == null))
                {
                    this._NetworkButton = this.transform.Find("ButtonPanel").Find("Buttons").Find("NetworkButton").GetComponent<Button>();
                }
                return this._NetworkButton;
            }
        }
        [SerializeField()]
        private Button _BuildButton;
        public Button BuildButton
        {
            get
            {
                if ((this._BuildButton == null))
                {
                    this._BuildButton = this.transform.Find("ButtonPanel").Find("Buttons").Find("BuildButton").GetComponent<Button>();
                }
                return this._BuildButton;
            }
        }
        [SerializeField()]
        private Button _QuitButton;
        public Button QuitButton
        {
            get
            {
                if ((this._QuitButton == null))
                {
                    this._QuitButton = this.transform.Find("ButtonPanel").Find("Buttons").Find("QuitButton").GetComponent<Button>();
                }
                return this._QuitButton;
            }
        }
        [SerializeField()]
        private Image _LogoImage;
        public Image LogoImage
        {
            get
            {
                if ((this._LogoImage == null))
                {
                    this._LogoImage = this.transform.Find("Logo").GetComponent<Image>();
                }
                return this._LogoImage;
            }
        }
        partial void onAwake();
    }
}
