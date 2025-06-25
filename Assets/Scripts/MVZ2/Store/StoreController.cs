using System;
using System.Collections.Generic;
using System.Linq;
using MukioI18n;
using MVZ2.Managers;
using MVZ2.Metas;
using MVZ2.Saves;
using MVZ2.Scenes;
using MVZ2.Talk;
using MVZ2.Talks;
using MVZ2.Vanilla;
using MVZ2.Vanilla.Audios;
using MVZ2.Vanilla.Saves;
using PVZEngine;
using Tools;
using UnityEngine;

namespace MVZ2.Store
{
    public class StoreController : MainScenePage
    {
        public override void Display()
        {
            base.Display();
            page = 0;
            chatFadeTimeout = 0;
            UpdateProducts();
            UpdatePage();
            ResetChatTimeout();
            UpdateMoney();
            ui.Display();


            var presets = Main.ResourceManager.GetAllStorePresets();
            var filteredPresets = presets.Where(p => p.Conditions == null || Main.SaveManager.MeetsXMLConditions(p.Conditions));
            var preset = filteredPresets.OrderByDescending(p => p.Priority).FirstOrDefault();
            SetPreset(preset);
        }
        public async void CheckStartTalks()
        {
            var loreTalks = Main.ResourceManager.GetCurrentStoreLoreTalks();
            if (loreTalks == null)
                return;
            // �Ի�
            var queue = new Queue<NamespaceID>();
            foreach (var lore in loreTalks)
            {
                if (!queue.Contains(lore))
                    queue.Enqueue(lore);
            }

            while (queue.Count > 0)
            {
                var talk = queue.Dequeue();
                if (!NamespaceID.IsValid(talk))
                    continue;
                ui.SetStoreUIVisible(false);
                await talkController.SimpleStartTalkAsync(talk, 0, 1);
            }
            ui.SetStoreUIVisible(true);
        }
        public void SetPreset(StorePresetMeta preset)
        {
            characterId = preset.Character;
            var backgroundSprite = Main.GetFinalSprite(preset.Background);

            var viewData = Main.ResourceManager.GetCharacterViewData(characterId, null, CharacterSide.Left);
            ui.SetCharacter(viewData);
            ui.SetBackground(backgroundSprite);
            if (!Main.MusicManager.IsPlaying(preset.Music))
                Main.MusicManager.Play(preset.Music);
        }

        #region ��������
        private void Awake()
        {
            ui.OnReturnClick += OnReturnClickCallback;
            ui.OnPageButtonClick += OnPageButtonClickCallback;
            ui.OnProductPointerEnter += OnProductPointerEnterCallback;
            ui.OnProductPointerExit += OnProductPointerExitCallback;
            ui.OnProductClick += OnProductClickCallback;

            chatRNG = new RandomGenerator(new Guid().GetHashCode());
        }
        private void Update()
        {
            if (!pointingProduct && !talkController.IsTalking)
            {
                chatTimeout -= Time.deltaTime;
                if (chatTimeout <= 0)
                {
                    ShowChat();
                    ResetChatTimeout();
                    chatFadeTimeout = MAX_CHAT_FADE_TIMEOUT;
                }
                chatFadeTimeout -= Time.deltaTime;
                if (chatFadeTimeout <= 0)
                {
                    ui.HideTalk();
                }
            }
            cameraShakeRoot.localPosition = (Vector3)Main.ShakeManager.GetShake2D();
        }
        #endregion

        #region UI �¼��ص�
        private void OnReturnClickCallback()
        {
            Hide();
            OnReturnClick?.Invoke();
        }
        private void OnPageButtonClickCallback(bool next)
        {
            var offset = next ? 1 : -1;
            page += offset;
            var totalPages = GetTotalPages();
            if (page < 0)
            {
                page = totalPages - 1;
            }
            else if (page >= totalPages)
            {
                page = 0;
            }
            UpdatePage();
        }
        private void OnProductPointerEnterCallback(int index)
        {
            var product = GetCurrentProduct(index);
            if (!NamespaceID.IsValid(product))
                return;
            var productMeta = Main.ResourceManager.GetProductMeta(product);
            var textKey = productMeta.GetMessage(characterId);
            var message = GetTranslatedString(VanillaStrings.CONTEXT_STORE_TALK, textKey);
            pointingProduct = true;
            ui.ShowTalk(message);
        }
        private void OnProductPointerExitCallback(int index)
        {
            pointingProduct = false;
            ui.HideTalk();
        }
        private void OnProductClickCallback(int index)
        {
            var product = GetCurrentProduct(index);
            if (!NamespaceID.IsValid(product))
                return;
            var productMeta = Main.ResourceManager.GetProductMeta(product);
            var stage = Main.StoreManager.GetCurrentProductStage(productMeta);
            if (Main.StoreManager.IsSoldout(stage))
                return;
            var price = stage.Price;
            var money = Main.SaveManager.GetMoney();
            if (money >= price)
            {
                var title = Main.LanguageManager._(PURCHASE);
                var desc = Main.LanguageManager._n(PURCHASE_DESCRIPTION, price, price);
                Main.Scene.ShowDialogSelect(title, desc, (purchase) =>
                {
                    if (purchase)
                    {
                        Main.SaveManager.AddMoney(-price);
                        Main.SoundManager.Play2D(VanillaSoundID.cashRegister);
                        Main.SaveManager.Unlock(stage.Unlocks);
                        Main.SaveManager.SaveToFile(); // ������Ʒ�󱣴���Ϸ
                        UpdateMoney();
                        UpdatePage();
                    }
                });
            }
            else
            {
                var title = Main.LanguageManager._(INSUFFICIENT_MONEY);
                var desc = Main.LanguageManager._(INSUFFICIENT_MONEY_DESCRIPTION);
                Main.Scene.ShowDialogMessage(title, desc);
            }
        }
        #endregion
        private void UpdateProducts()
        {
            products.Clear();
            var productEntries = Main.SaveManager.GetUnlockedProducts();
            Main.StoreManager.GetOrderedProducts(productEntries, productsPerRow, products);
        }
        private void UpdatePage()
        {
            var viewDatas = products.Skip(page * productsPerPage).Take(productsPerPage).Select(c => Main.StoreManager.GetProductViewData(c)).ToArray();
            ui.SetProducts(viewDatas);

            var totalPages = GetTotalPages();
            bool interactable = totalPages > 1;
            ui.SetPageButtonInteractable(interactable, interactable);

            ui.SetPageNumber(Main.LanguageManager._(PAGE_TEMPLATE, page + 1, totalPages));
        }
        private int GetTotalPages()
        {
            return Mathf.CeilToInt(products.Count / (float)productsPerPage);
        }
        private void ShowChat()
        {
            var chat = Main.StoreManager.GetRandomChat(characterId, chatRNG);
            if (chat == null)
                return;
            var message = GetTranslatedString(VanillaStrings.CONTEXT_STORE_TALK, chat.Text);
            ui.ShowTalk(message);
            Main.SoundManager.Play2D(chat.Sound);
        }
        private void UpdateMoney()
        {
            ui.SetMoney(Main.SaveManager.GetMoney().ToString("N0"));
        }
        private NamespaceID GetCurrentProduct(int index)
        {
            var i = page * productsPerPage + index;
            if (i < 0 || i >= products.Count)
                return null;
            return products[i];
        }
        private void ResetChatTimeout()
        {
            chatTimeout = MAX_CHAT_TIMEOUT;
        }
        private string GetTranslatedString(string context, string text, params object[] args)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            return Main.LanguageManager._p(context, text, args);
        }
        public event Action OnReturnClick;

        [TranslateMsg("商店对话框标题")]
        public const string PURCHASE = "购买物品";
        [TranslateMsg("商店对话框内容，{0}为价格", selfPlural: true)]
        public const string PURCHASE_DESCRIPTION = "确定以{0:N0}的价格买下这个物品？";
        [TranslateMsg("商店对话框标题")]
        public const string INSUFFICIENT_MONEY = "金钱不足";
        [TranslateMsg("商店对话框内容")]
        public const string INSUFFICIENT_MONEY_DESCRIPTION = "你没有足够的金钱！";
        [TranslateMsg("商店的页面计数，{0}为当前页面，{1}为总页面")]
        public const string PAGE_TEMPLATE = "{0}/{1}";
        private MainManager Main => MainManager.Instance;
        private List<NamespaceID> products = new List<NamespaceID>();
        private const float MAX_CHAT_TIMEOUT = 10;
        private const float MAX_CHAT_FADE_TIMEOUT = 5;
        private float chatTimeout;
        private float chatFadeTimeout;
        private NamespaceID characterId;
        private bool pointingProduct;
        private int page;
        private RandomGenerator chatRNG;


        [SerializeField]
        private Transform cameraShakeRoot;
        [SerializeField]
        private StoreUI ui;
        [SerializeField]
        private TalkController talkController;
        [SerializeField]
        private int productsPerRow = 4;
        [SerializeField]
        private int productsPerPage = 8;
    }
}
