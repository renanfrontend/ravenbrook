using System.Collections.Generic;

namespace Ravenbrook
{
    /// <summary>
    /// Textos do jogo em 13 idiomas. Ordem das colunas igual a <see cref="Names"/>.
    /// Entrada sem tradução (null) usa o inglês.
    /// </summary>
    public static class Loc
    {
        public static readonly string[] Names =
        {
            "Português", "English", "Español", "Français", "Deutsch", "Italiano", "Русский",
            "Polski", "Türkçe", "Nederlands", "日本語", "한국어", "简体中文"
        };

        public static int Lang;
        public static bool IsPt => Lang == 0;

        static Dictionary<string, string[]> table;

        public static string Get(string key)
        {
            if (table == null) Build();
            if (!table.TryGetValue(key, out var row)) return key;
            string s = Lang < row.Length ? row[Lang] : null;
            return string.IsNullOrEmpty(s) ? row[1] : s;
        }

        public static string F(string key, params object[] args) => string.Format(Get(key), args);

        static void A(string key, params string[] values) => table[key] = values;

        static void Build()
        {
            table = new Dictionary<string, string[]>();
            //        pt, en, es, fr, de, it, ru, pl, tr, nl, ja, ko, zh
            A("subtitle", "O SURTO", "OUTBREAK", "EL BROTE", "L'ÉPIDÉMIE", "DER AUSBRUCH", "L'EPIDEMIA", "ВСПЫШКА", "EPIDEMIA", "SALGIN", "DE UITBRAAK", "アウトブレイク", "아웃브레이크", "爆发");
            A("new_game", "Novo jogo", "New game", "Nueva partida", "Nouvelle partie", "Neues Spiel", "Nuova partita", "Новая игра", "Nowa gra", "Yeni oyun", "Nieuw spel", "ニューゲーム", "새 게임", "新游戏");
            A("continue", "Continuar", "Continue", "Continuar", "Continuer", "Fortsetzen", "Continua", "Продолжить", "Kontynuuj", "Devam et", "Doorgaan", "コンティニュー", "이어하기", "继续");
            A("options", "Opções", "Options", "Opciones", "Options", "Optionen", "Opzioni", "Настройки", "Opcje", "Ayarlar", "Opties", "オプション", "옵션", "选项");
            A("quit", "Sair", "Quit", "Salir", "Quitter", "Beenden", "Esci", "Выход", "Wyjdź", "Çıkış", "Afsluiten", "終了", "종료", "退出");
            A("credits", "Créditos", "Credits", "Créditos", "Crédits", "Credits", "Crediti", "Авторы", "Autorzy", "Emeği geçenler", "Credits", "クレジット", "크레딧", "制作人员");
            A("resume", "Voltar ao jogo", "Resume", "Reanudar", "Reprendre", "Weiter", "Riprendi", "Вернуться", "Wznów", "Devam", "Hervatten", "再開", "계속하기", "继续游戏");
            A("main_menu", "Menu principal", "Main menu", "Menú principal", "Menu principal", "Hauptmenü", "Menu principale", "Главное меню", "Menu główne", "Ana menü", "Hoofdmenu", "メインメニュー", "메인 메뉴", "主菜单");
            A("language", "Idioma", "Language", "Idioma", "Langue", "Sprache", "Lingua", "Язык", "Język", "Dil", "Taal", "言語", "언어", "语言");
            A("sensitivity", "Sensibilidade do mouse", "Mouse sensitivity", "Sensibilidad del ratón", "Sensibilité de la souris", "Mausempfindlichkeit", "Sensibilità mouse", "Чувствительность мыши", "Czułość myszy", "Fare hassasiyeti", "Muisgevoeligheid", "マウス感度", "마우스 감도", "鼠标灵敏度");
            A("volume", "Volume", "Volume", "Volumen", "Volume", "Lautstärke", "Volume", "Громкость", "Głośność", "Ses", "Volume", "音量", "볼륨", "音量");
            A("quality", "Gráficos", "Graphics", "Gráficos", "Graphismes", "Grafik", "Grafica", "Графика", "Grafika", "Grafik", "Graphics", "グラフィック", "그래픽", "画质");
            A("low", "Baixo", "Low", "Bajo", "Bas", "Niedrig", "Bassa", "Низкая", "Niska", "Düşük", "Laag", "低", "낮음", "低");
            A("medium", "Médio", "Medium", "Medio", "Moyen", "Mittel", "Media", "Средняя", "Średnia", "Orta", "Middel", "中", "중간", "中");
            A("high", "Alto", "High", "Alto", "Élevé", "Hoch", "Alta", "Высокая", "Wysoka", "Yüksek", "Hoog", "高", "높음", "高");
            A("back", "Voltar", "Back", "Atrás", "Retour", "Zurück", "Indietro", "Назад", "Wstecz", "Geri", "Terug", "戻る", "뒤로", "返回");
            A("dev_mode", "Painel Overwatch (F1)", "Overwatch panel (F1)", "Panel Overwatch (F1)", "Panneau Overwatch (F1)", "Overwatch-Panel (F1)", "Pannello Overwatch (F1)", "Панель Overwatch (F1)", "Panel Overwatch (F1)", "Overwatch paneli (F1)", "Overwatch-paneel (F1)", "Overwatchパネル (F1)", "Overwatch 패널 (F1)", "Overwatch 面板 (F1)");
            A("on", "Ligado", "On", "Activado", "Activé", "An", "Attivo", "Вкл.", "Wł.", "Açık", "Aan", "オン", "켜짐", "开");
            A("off", "Desligado", "Off", "Desactivado", "Désactivé", "Aus", "Disattivo", "Выкл.", "Wył.", "Kapalı", "Uit", "オフ", "꺼짐", "关");

            A("fine", "BEM", "FINE", "BIEN", "BIEN", "GUT", "BENE", "НОРМА", "DOBRZE", "İYİ", "GOED", "ファイン", "양호", "良好");
            A("caution", "CUIDADO", "CAUTION", "PRECAUCIÓN", "ATTENTION", "VORSICHT", "ATTENZIONE", "ОСТОРОЖНО", "UWAGA", "DİKKAT", "LET OP", "コーション", "주의", "注意");
            A("danger", "PERIGO", "DANGER", "PELIGRO", "DANGER", "GEFAHR", "PERICOLO", "ОПАСНОСТЬ", "ZAGROŻENIE", "TEHLİKE", "GEVAAR", "デンジャー", "위험", "危险");
            A("open", "Abrir", "Open", "Abrir", "Ouvrir", "Öffnen", "Apri", "Открыть", "Otwórz", "Aç", "Openen", "開ける", "열기", "打开");
            A("take", "Pegar", "Take", "Recoger", "Prendre", "Nehmen", "Raccogli", "Взять", "Weź", "Al", "Pakken", "拾う", "줍기", "拾取");
            A("read", "Ler", "Read", "Leer", "Lire", "Lesen", "Leggi", "Читать", "Czytaj", "Oku", "Lezen", "読む", "읽기", "阅读");
            A("use", "Usar", "Use", "Usar", "Utiliser", "Benutzen", "Usa", "Использовать", "Użyj", "Kullan", "Gebruiken", "使う", "사용", "使用");
            A("combine", "Combinar", "Combine", "Combinar", "Combiner", "Kombinieren", "Combina", "Объединить", "Połącz", "Birleştir", "Combineren", "組み合わせ", "조합", "组合");
            A("examine", "Examinar", "Examine", "Examinar", "Examiner", "Untersuchen", "Esamina", "Осмотреть", "Zbadaj", "İncele", "Bekijken", "調べる", "조사", "检查");
            A("save", "Salvar", "Save", "Guardar", "Sauvegarder", "Speichern", "Salva", "Сохранить", "Zapisz", "Kaydet", "Opslaan", "セーブ", "저장", "保存");
            A("item_box", "Baú de itens", "Item box", "Baúl de objetos", "Coffre", "Kiste", "Baule", "Ящик для предметов", "Skrzynia", "Eşya sandığı", "Opslagkist", "アイテムボックス", "아이템 상자", "物品箱");
            A("statue", "Estátua", "Statue", "Estatua", "Statue", "Statue", "Statua", "Статуя", "Posąg", "Heykel", "Standbeeld", "像", "석상", "雕像");
            A("locked", "Trancada. Requer: {0}", "Locked. Requires: {0}", "Cerrada. Requiere: {0}", "Verrouillée. Requiert : {0}", "Verschlossen. Benötigt: {0}", "Chiusa. Serve: {0}", "Заперто. Нужно: {0}", "Zamknięte. Wymaga: {0}", "Kilitli. Gerekli: {0}", "Op slot. Vereist: {0}", "鍵がかかっている。必要: {0}", "잠겨 있다. 필요: {0}", "锁住了。需要：{0}");
            A("unlocked", "Você usou: {0}", "You used: {0}", "Usaste: {0}", "Vous avez utilisé : {0}", "Benutzt: {0}", "Hai usato: {0}", "Использовано: {0}", "Użyto: {0}", "Kullanıldı: {0}", "Gebruikt: {0}", "{0}を使った", "{0}을(를) 사용했다", "使用了：{0}");
            A("picked", "Obtido: {0}", "Got: {0}", "Obtenido: {0}", "Obtenu : {0}", "Erhalten: {0}", "Ottenuto: {0}", "Получено: {0}", "Zdobyto: {0}", "Alındı: {0}", "Verkregen: {0}", "{0}を手に入れた", "{0} 획득", "获得：{0}");
            A("inv_full", "Inventário cheio", "Inventory full", "Inventario lleno", "Inventaire plein", "Inventar voll", "Inventario pieno", "Инвентарь полон", "Ekwipunek pełny", "Envanter dolu", "Inventaris vol", "持ち物がいっぱいだ", "인벤토리가 가득 찼다", "物品栏已满");
            A("saved", "Progresso salvo", "Progress saved", "Progreso guardado", "Progression sauvegardée", "Fortschritt gespeichert", "Progressi salvati", "Прогресс сохранён", "Postęp zapisany", "İlerleme kaydedildi", "Voortgang opgeslagen", "セーブしました", "저장되었다", "进度已保存");
            A("no_ammo", "Sem munição", "No ammo", "Sin munición", "Plus de munitions", "Keine Munition", "Niente munizioni", "Нет патронов", "Brak amunicji", "Mermi yok", "Geen munitie", "弾がない", "탄약 없음", "没有弹药");
            A("reloading", "Recarregando...", "Reloading...", "Recargando...", "Rechargement...", "Nachladen...", "Ricarica...", "Перезарядка...", "Przeładowanie...", "Şarjör değiştiriliyor...", "Herladen...", "リロード中...", "재장전 중...", "装填中...");
            A("knife_broken", "A faca quebrou", "The knife broke", "El cuchillo se rompió", "Le couteau s'est brisé", "Das Messer ist zerbrochen", "Il coltello si è rotto", "Нож сломался", "Nóż się złamał", "Bıçak kırıldı", "Het mes is gebroken", "ナイフが壊れた", "칼이 부러졌다", "刀坏了");
            A("hp_full", "Sua saúde já está boa", "Your health is already fine", "Tu salud ya está bien", "Votre santé est déjà bonne", "Deine Gesundheit ist bereits gut", "La salute è già al massimo", "Здоровье и так в норме", "Zdrowie jest już pełne", "Sağlığın zaten iyi", "Je gezondheid is al goed", "体力は十分だ", "체력이 충분하다", "体力已满");
            A("need_green", "Combine com uma erva verde", "Combine it with a green herb", "Combínala con una hierba verde", "Combinez-la avec une herbe verte", "Mit einem grünen Kraut kombinieren", "Combinala con un'erba verde", "Смешайте с зелёной травой", "Połącz z zielonym zielem", "Yeşil bitkiyle birleştir", "Combineer met een groen kruid", "グリーンハーブと組み合わせよう", "녹색 허브와 조합하자", "与绿色草药组合");
            A("combined", "Itens combinados", "Items combined", "Objetos combinados", "Objets combinés", "Gegenstände kombiniert", "Oggetti combinati", "Предметы объединены", "Połączono przedmioty", "Eşyalar birleştirildi", "Voorwerpen gecombineerd", "組み合わせた", "조합했다", "已组合");
            A("cant_combine", "Não dá para combinar", "These can't be combined", "No se pueden combinar", "Impossible de combiner", "Nicht kombinierbar", "Non si possono combinare", "Нельзя объединить", "Nie można połączyć", "Birleştirilemez", "Kan niet worden gecombineerd", "組み合わせられない", "조합할 수 없다", "无法组合");
            A("statue_need", "Faltam {0} medalhões", "{0} medallions missing", "Faltan {0} medallones", "Il manque {0} médaillons", "Es fehlen {0} Medaillons", "Mancano {0} medaglioni", "Не хватает медальонов: {0}", "Brakuje medalionów: {0}", "{0} madalyon eksik", "Nog {0} medaillons nodig", "メダルがあと{0}個必要だ", "메달 {0}개가 부족하다", "还缺{0}枚徽章");
            A("statue_hint", "Uma estátua com três encaixes vazios", "A statue with three empty slots", "Una estatua con tres huecos vacíos", "Une statue avec trois emplacements vides", "Eine Statue mit drei leeren Fassungen", "Una statua con tre incavi vuoti", "Статуя с тремя пустыми гнёздами", "Posąg z trzema pustymi gniazdami", "Üç boş yuvalı bir heykel", "Een standbeeld met drie lege uitsparingen", "三つのくぼみがある像だ", "세 개의 빈 홈이 있는 석상이다", "一座有三个空槽的雕像");
            A("medal_placed", "Medalhão encaixado", "Medallion placed", "Medallón colocado", "Médaillon placé", "Medaillon eingesetzt", "Medaglione inserito", "Медальон установлен", "Medalion umieszczony", "Madalyon yerleştirildi", "Medaillon geplaatst", "メダルをはめ込んだ", "메달을 끼웠다", "徽章已放入");
            A("gate_open", "O portão da garagem se abriu", "The garage gate opened", "La puerta del garaje se abrió", "La porte du garage s'est ouverte", "Das Garagentor hat sich geöffnet", "Il cancello del garage si è aperto", "Ворота гаража открылись", "Brama garażu się otworzyła", "Garaj kapısı açıldı", "De garagepoort is open", "ガレージのシャッターが開いた", "차고 셔터가 열렸다", "车库大门打开了");
            A("gate_closed", "O portão não abre. Talvez a estátua do saguão...", "The gate won't open. Maybe the lobby statue...", "La puerta no abre. Quizás la estatua del vestíbulo...", "La porte ne s'ouvre pas. Peut-être la statue du hall...", "Das Tor geht nicht auf. Vielleicht die Statue in der Halle...", "Il cancello non si apre. Forse la statua dell'atrio...", "Ворота не открываются. Может, статуя в холле...", "Brama się nie otwiera. Może posąg w holu...", "Kapı açılmıyor. Belki lobideki heykel...", "De poort gaat niet open. Misschien het beeld in de hal...", "シャッターは開かない。ロビーの像が関係あるかも...", "셔터가 열리지 않는다. 로비의 석상이 관련 있을지도...", "大门打不开。也许和大厅的雕像有关……");
            A("pursuer", "Passos pesados ecoam pela delegacia...", "Heavy footsteps echo through the station...", "Pasos pesados resuenan en la comisaría...", "Des pas lourds résonnent dans le commissariat...", "Schwere Schritte hallen durch das Revier...", "Passi pesanti echeggiano nella stazione...", "По участку разносятся тяжёлые шаги...", "Ciężkie kroki niosą się po komisariacie...", "Karakolda ağır adımlar yankılanıyor...", "Zware voetstappen galmen door het bureau...", "重い足音が署内に響く...", "무거운 발소리가 경찰서에 울린다...", "沉重的脚步声在警局里回荡……");
            A("you_died", "VOCÊ MORREU", "YOU DIED", "HAS MUERTO", "VOUS ÊTES MORT", "DU BIST TOT", "SEI MORTO", "ВЫ ПОГИБЛИ", "ZGINĄŁEŚ", "ÖLDÜN", "JE BENT DOOD", "死亡", "사망", "你死了");
            A("retry", "Recomeçar", "Restart", "Reiniciar", "Recommencer", "Neu starten", "Ricomincia", "Начать заново", "Zacznij od nowa", "Yeniden başla", "Opnieuw beginnen", "最初から", "처음부터", "重新开始");
            A("victory", "VOCÊ ESCAPOU", "YOU ESCAPED", "HAS ESCAPADO", "VOUS AVEZ FUI", "DU BIST ENTKOMMEN", "SEI FUGGITO", "ВЫ СБЕЖАЛИ", "UCIEKŁEŚ", "KAÇTIN", "JE BENT ONTSNAPT", "脱出成功", "탈출 성공", "你逃出来了");
            A("inventory", "Inventário", "Inventory", "Inventario", "Inventaire", "Inventar", "Inventario", "Инвентарь", "Ekwipunek", "Envanter", "Inventaris", "持ち物", "인벤토리", "物品栏");
            A("map", "Mapa", "Map", "Mapa", "Carte", "Karte", "Mappa", "Карта", "Mapa", "Harita", "Kaart", "マップ", "지도", "地图");
            A("files", "Arquivos", "Files", "Archivos", "Dossiers", "Akten", "Documenti", "Документы", "Akta", "Dosyalar", "Dossiers", "ファイル", "파일", "文件");
            A("objective", "Objetivo", "Objective", "Objetivo", "Objectif", "Ziel", "Obiettivo", "Цель", "Cel", "Hedef", "Doel", "目的", "목표", "目标");
            A("obj_explore", "Explore a delegacia e encontre uma saída", "Explore the station and find a way out", "Explora la comisaría y encuentra una salida", "Explorez le commissariat et trouvez une sortie", "Erkunde das Revier und finde einen Ausweg", "Esplora la stazione e trova un'uscita", "Обыщите участок и найдите выход", "Przeszukaj komisariat i znajdź wyjście", "Karakolu keşfet ve bir çıkış bul", "Verken het bureau en vind een uitweg", "警察署を探索し、出口を見つけろ", "경찰서를 탐색하고 출구를 찾아라", "探索警局并找到出口");
            A("obj_medals", "Leve os 3 medalhões à estátua do saguão ({0}/3)", "Bring the 3 medallions to the lobby statue ({0}/3)", "Lleva los 3 medallones a la estatua del vestíbulo ({0}/3)", "Apportez les 3 médaillons à la statue du hall ({0}/3)", "Bring die 3 Medaillons zur Statue in der Halle ({0}/3)", "Porta i 3 medaglioni alla statua dell'atrio ({0}/3)", "Принесите 3 медальона к статуе в холле ({0}/3)", "Zanieś 3 medaliony do posągu w holu ({0}/3)", "3 madalyonu lobideki heykele götür ({0}/3)", "Breng de 3 medaillons naar het beeld in de hal ({0}/3)", "3つのメダルをロビーの像へ ({0}/3)", "메달 3개를 로비 석상에 가져가라 ({0}/3)", "把3枚徽章带到大厅雕像处（{0}/3）");
            A("obj_escape", "Fuja pela garagem", "Escape through the garage", "Escapa por el garaje", "Fuyez par le garage", "Flieh durch die Garage", "Fuggi dal garage", "Бегите через гараж", "Uciekaj przez garaż", "Garajdan kaç", "Vlucht via de garage", "ガレージから脱出しろ", "차고로 탈출하라", "从车库逃离");
            A("controls",
                "WASD mover · Shift correr · Botão direito mirar · Botão esquerdo atirar · R recarregar · E interagir · Q virar · F faca · 1-4 armas · Tab inventário · M mapa · L lanterna · Esc pausa",
                "WASD move · Shift run · Right mouse aim · Left mouse shoot · R reload · E interact · Q quick turn · F knife · 1-4 weapons · Tab inventory · M map · L flashlight · Esc pause",
                "WASD mover · Shift correr · Clic der. apuntar · Clic izq. disparar · R recargar · E interactuar · Q girar · F cuchillo · 1-4 armas · Tab inventario · M mapa · L linterna · Esc pausa",
                "WASD bouger · Maj courir · Clic droit viser · Clic gauche tirer · R recharger · E interagir · Q demi-tour · F couteau · 1-4 armes · Tab inventaire · M carte · L lampe · Échap pause",
                "WASD bewegen · Umschalt rennen · Rechte Maus zielen · Linke Maus schießen · R nachladen · E interagieren · Q umdrehen · F Messer · 1-4 Waffen · Tab Inventar · M Karte · L Lampe · Esc Pause",
                "WASD muovi · Shift corri · Tasto destro mira · Tasto sinistro spara · R ricarica · E interagisci · Q gira · F coltello · 1-4 armi · Tab inventario · M mappa · L torcia · Esc pausa",
                "WASD движение · Shift бег · ПКМ прицел · ЛКМ огонь · R перезарядка · E действие · Q разворот · F нож · 1-4 оружие · Tab инвентарь · M карта · L фонарь · Esc пауза",
                "WASD ruch · Shift bieg · PPM celowanie · LPM strzał · R przeładuj · E interakcja · Q obrót · F nóż · 1-4 broń · Tab ekwipunek · M mapa · L latarka · Esc pauza",
                "WASD hareket · Shift koş · Sağ tık nişan · Sol tık ateş · R şarjör · E etkileşim · Q dön · F bıçak · 1-4 silahlar · Tab envanter · M harita · L fener · Esc duraklat",
                "WASD bewegen · Shift rennen · Rechtermuis richten · Linkermuis schieten · R herladen · E gebruiken · Q omdraaien · F mes · 1-4 wapens · Tab inventaris · M kaart · L zaklamp · Esc pauze",
                "WASD 移動 · Shift ダッシュ · 右クリック 構え · 左クリック 射撃 · R リロード · E 調べる · Q 180度ターン · F ナイフ · 1-4 武器 · Tab 持ち物 · M マップ · L ライト · Esc ポーズ",
                "WASD 이동 · Shift 달리기 · 우클릭 조준 · 좌클릭 사격 · R 재장전 · E 상호작용 · Q 뒤돌기 · F 칼 · 1-4 무기 · Tab 인벤토리 · M 지도 · L 손전등 · Esc 일시정지",
                "WASD 移动 · Shift 奔跑 · 右键 瞄准 · 左键 射击 · R 装填 · E 互动 · Q 快速转身 · F 小刀 · 1-4 武器 · Tab 物品栏 · M 地图 · L 手电筒 · Esc 暂停");
            A("stats", "Tempo {0} · Abates {1} · Salvamentos {2}", "Time {0} · Kills {1} · Saves {2}", "Tiempo {0} · Bajas {1} · Guardados {2}", "Temps {0} · Éliminations {1} · Sauvegardes {2}", "Zeit {0} · Kills {1} · Speicherungen {2}", "Tempo {0} · Uccisioni {1} · Salvataggi {2}", "Время {0} · Убито {1} · Сохранений {2}", "Czas {0} · Zabici {1} · Zapisy {2}", "Süre {0} · Öldürme {1} · Kayıt {2}", "Tijd {0} · Kills {1} · Opslagen {2}", "タイム {0} · 撃破 {1} · セーブ {2}", "시간 {0} · 처치 {1} · 저장 {2}", "时间 {0} · 击杀 {1} · 存档 {2}");
            A("rank", "Classificação {0}", "Rank {0}", "Rango {0}", "Rang {0}", "Rang {0}", "Grado {0}", "Ранг {0}", "Ranga {0}", "Derece {0}", "Rang {0}", "ランク {0}", "랭크 {0}", "评级 {0}");
            A("cheats_flag", "Painel Overwatch usado: partida de teste, sem classificação", "Overwatch panel used: test run, no rank");
            A("choose_combine", "Escolha o segundo item", "Choose the second item", "Elige el segundo objeto", "Choisissez le second objet", "Wähle den zweiten Gegenstand", "Scegli il secondo oggetto", "Выберите второй предмет", "Wybierz drugi przedmiot", "İkinci eşyayı seç", "Kies het tweede voorwerp", "2つ目のアイテムを選択", "두 번째 아이템을 선택", "选择第二个物品");
            A("store", "Guardar", "Store", "Guardar", "Ranger", "Einlagern", "Deposita", "Положить", "Odłóż", "Bırak", "Opbergen", "預ける", "맡기기", "存入");
            A("withdraw", "Retirar", "Take out", "Sacar", "Retirer", "Entnehmen", "Preleva", "Забрать", "Wyjmij", "Al", "Pakken", "取り出す", "꺼내기", "取出");
            A("close", "Fechar", "Close", "Cerrar", "Fermer", "Schließen", "Chiudi", "Закрыть", "Zamknij", "Kapat", "Sluiten", "閉じる", "닫기", "关闭");
            A("legend_visited", "Visitado (itens restantes)", "Visited (items left)", "Visitado (quedan objetos)", "Visité (objets restants)", "Besucht (Gegenstände übrig)", "Visitato (oggetti rimasti)", "Посещено (есть предметы)", "Odwiedzone (zostały przedmioty)", "Ziyaret edildi (eşya var)", "Bezocht (voorwerpen over)", "探索済み(アイテムあり)", "방문함(아이템 남음)", "已探索（有物品）");
            A("legend_cleared", "Visitado (vazio)", "Visited (cleared)", "Visitado (vacío)", "Visité (vide)", "Besucht (leer)", "Visitato (vuoto)", "Посещено (пусто)", "Odwiedzone (puste)", "Ziyaret edildi (boş)", "Bezocht (leeg)", "探索済み(なし)", "방문함(없음)", "已探索（无物品）");
            A("legend_locked", "Porta trancada", "Locked door", "Puerta cerrada", "Porte verrouillée", "Verschlossene Tür", "Porta chiusa", "Запертая дверь", "Zamknięte drzwi", "Kilitli kapı", "Afgesloten deur", "施錠された扉", "잠긴 문", "锁着的门");
            A("durability", "Durabilidade", "Durability", "Durabilidad", "Durabilité", "Haltbarkeit", "Durabilità", "Прочность", "Wytrzymałość", "Dayanıklılık", "Duurzaamheid", "耐久度", "내구도", "耐久度");

            A("room_archive", "Arquivo", "Archive", "Archivo", "Archives", "Archiv", "Archivio", "Архив", "Archiwum", "Arşiv", "Archief", "資料室", "자료실", "档案室");
            A("room_library", "Biblioteca", "Library", "Biblioteca", "Bibliothèque", "Bibliothek", "Biblioteca", "Библиотека", "Biblioteka", "Kütüphane", "Bibliotheek", "図書室", "도서관", "图书馆");
            A("room_records", "Registros", "Records room", "Registros", "Salle des registres", "Registratur", "Registri", "Картотека", "Rejestry", "Kayıt odası", "Registratie", "記録室", "기록실", "记录室");
            A("room_west", "Escritório oeste", "West office", "Oficina oeste", "Bureau ouest", "Westbüro", "Ufficio ovest", "Западный офис", "Biuro zachodnie", "Batı ofisi", "Westkantoor", "西オフィス", "서쪽 사무실", "西办公室");
            A("room_lobby", "Saguão", "Main hall", "Vestíbulo", "Hall principal", "Haupthalle", "Atrio", "Главный холл", "Hol główny", "Ana salon", "Hoofdhal", "ロビー", "로비", "大厅");
            A("room_east", "Escritório leste", "East office", "Oficina este", "Bureau est", "Ostbüro", "Ufficio est", "Восточный офис", "Biuro wschodnie", "Doğu ofisi", "Oostkantoor", "東オフィス", "동쪽 사무실", "东办公室");
            A("room_cells", "Celas", "Cell block", "Celdas", "Cellules", "Zellentrakt", "Celle", "Камеры", "Cele", "Hücreler", "Cellenblok", "留置場", "유치장", "牢房");
            A("room_safe", "Sala segura", "Safe room", "Sala segura", "Salle sûre", "Sicherer Raum", "Stanza sicura", "Безопасная комната", "Bezpieczny pokój", "Güvenli oda", "Veilige kamer", "セーフルーム", "안전한 방", "安全屋");
            A("room_garage", "Garagem", "Garage", "Garaje", "Garage", "Garage", "Garage", "Гараж", "Garaż", "Garaj", "Garage", "ガレージ", "차고", "车库");

            A("it_herb_g", "Erva verde", "Green herb", "Hierba verde", "Herbe verte", "Grünes Kraut", "Erba verde", "Зелёная трава", "Zielone ziele", "Yeşil bitki", "Groen kruid", "グリーンハーブ", "녹색 허브", "绿色草药");
            A("it_herb_r", "Erva vermelha", "Red herb", "Hierba roja", "Herbe rouge", "Rotes Kraut", "Erba rossa", "Красная трава", "Czerwone ziele", "Kırmızı bitki", "Rood kruid", "レッドハーブ", "적색 허브", "红色草药");
            A("it_herb_m", "Ervas misturadas", "Mixed herbs", "Hierbas mezcladas", "Herbes mélangées", "Gemischte Kräuter", "Erbe miste", "Смесь трав", "Mieszanka ziół", "Karışık bitki", "Gemengde kruiden", "ミックスハーブ", "혼합 허브", "混合草药");
            A("it_spray", "Spray médico", "First aid spray", "Spray de primeros auxilios", "Spray de soins", "Erste-Hilfe-Spray", "Spray medico", "Аптечка-спрей", "Spray medyczny", "İlk yardım spreyi", "EHBO-spray", "救急スプレー", "구급 스프레이", "急救喷雾");
            A("it_ammo_p", "Balas de pistola", "Handgun ammo", "Munición de pistola", "Munitions de pistolet", "Pistolenmunition", "Munizioni pistola", "Патроны для пистолета", "Amunicja do pistoletu", "Tabanca mermisi", "Pistoolmunitie", "ハンドガンの弾", "권총 탄약", "手枪弹药");
            A("it_ammo_s", "Cartuchos de escopeta", "Shotgun shells", "Cartuchos de escopeta", "Cartouches de fusil", "Schrotpatronen", "Cartucce fucile", "Патроны для дробовика", "Naboje do strzelby", "Pompalı fişeği", "Hagelpatronen", "ショットガンの弾", "산탄총 탄약", "霰弹枪子弹");
            A("it_ammo_m", "Balas de magnum", "Magnum rounds", "Balas magnum", "Balles magnum", "Magnum-Munition", "Proiettili magnum", "Патроны магнума", "Amunicja magnum", "Magnum mermisi", "Magnummunitie", "マグナムの弾", "매그넘 탄약", "马格南子弹");
            A("it_key_crow", "Chave do Corvo", "Crow key", "Llave del cuervo", "Clé du corbeau", "Rabenschlüssel", "Chiave del corvo", "Ключ ворона", "Klucz kruka", "Karga anahtarı", "Kraaiensleutel", "カラスの鍵", "까마귀 열쇠", "乌鸦钥匙");
            A("it_key_lion", "Chave do Leão", "Lion key", "Llave del león", "Clé du lion", "Löwenschlüssel", "Chiave del leone", "Ключ льва", "Klucz lwa", "Aslan anahtarı", "Leeuwensleutel", "ライオンの鍵", "사자 열쇠", "狮子钥匙");
            A("it_medal_sun", "Medalhão do Sol", "Sun medallion", "Medallón del sol", "Médaillon du soleil", "Sonnenmedaillon", "Medaglione del sole", "Медальон солнца", "Medalion słońca", "Güneş madalyonu", "Zonnemedaillon", "太陽のメダル", "태양 메달", "太阳徽章");
            A("it_medal_moon", "Medalhão da Lua", "Moon medallion", "Medallón de la luna", "Médaillon de la lune", "Mondmedaillon", "Medaglione della luna", "Медальон луны", "Medalion księżyca", "Ay madalyonu", "Maanmedaillon", "月のメダル", "달 메달", "月亮徽章");
            A("it_medal_star", "Medalhão da Estrela", "Star medallion", "Medallón de la estrella", "Médaillon de l'étoile", "Sternmedaillon", "Medaglione della stella", "Медальон звезды", "Medalion gwiazdy", "Yıldız madalyonu", "Stermedaillon", "星のメダル", "별 메달", "星星徽章");
            A("w_knife", "Faca", "Knife", "Cuchillo", "Couteau", "Messer", "Coltello", "Нож", "Nóż", "Bıçak", "Mes", "ナイフ", "칼", "小刀");
            A("w_pistol", "Pistola", "Handgun", "Pistola", "Pistolet", "Pistole", "Pistola", "Пистолет", "Pistolet", "Tabanca", "Pistool", "ハンドガン", "권총", "手枪");
            A("w_shotgun", "Escopeta", "Shotgun", "Escopeta", "Fusil à pompe", "Schrotflinte", "Fucile a pompa", "Дробовик", "Strzelba", "Pompalı tüfek", "Jachtgeweer", "ショットガン", "산탄총", "霰弹枪");
            A("w_magnum", "Magnum", "Magnum", "Magnum", "Magnum", "Magnum", "Magnum", "Магнум", "Magnum", "Magnum", "Magnum", "マグナム", "매그넘", "马格南");

            // Descrições dos itens (português e inglês; os outros idiomas usam o inglês)
            A("it_herb_g_d", "Planta medicinal nativa de Ravenbrook. Recupera um pouco de saúde.", "A medicinal plant native to Ravenbrook. Restores some health.");
            A("it_herb_r_d", "Sozinha não faz nada. Misturada a uma erva verde, potencializa a cura.", "Useless on its own. Mixed with a green herb, it boosts healing.");
            A("it_herb_m_d", "Mistura de ervas verde e vermelha. Recupera toda a saúde.", "A mix of green and red herbs. Fully restores health.");
            A("it_spray_d", "Spray hemostático de uso militar. Recupera toda a saúde.", "Military-grade hemostatic spray. Fully restores health.");
            A("it_ammo_p_d", "Munição 9 mm para a pistola.", "9mm rounds for the handgun.");
            A("it_ammo_s_d", "Cartuchos calibre 12 para a escopeta.", "12-gauge shells for the shotgun.");
            A("it_ammo_m_d", "Munição .44 de alto impacto. Atravessa vários alvos.", "High-impact .44 rounds. Pierces through targets.");
            A("it_key_crow_d", "Chave de ferro com um corvo gravado. Abre a porta oeste do saguão.", "An iron key engraved with a crow. Opens the west door of the main hall.");
            A("it_key_lion_d", "Chave dourada com uma cabeça de leão. Abre a biblioteca.", "A golden key with a lion's head. Opens the library.");
            A("it_medal_sun_d", "Medalhão de bronze com um sol. Encaixa na estátua do saguão.", "A bronze medallion with a sun. Fits the main hall statue.");
            A("it_medal_moon_d", "Medalhão de prata com uma lua. Encaixa na estátua do saguão.", "A silver medallion with a moon. Fits the main hall statue.");
            A("it_medal_star_d", "Medalhão dourado com uma estrela. Encaixa na estátua do saguão.", "A golden medallion with a star. Fits the main hall statue.");

            // Painel Overwatch (modo de teste)
            A("ow_title", "OVERWATCH // PAINEL DE TESTE", "OVERWATCH // TEST PANEL");
            A("ow_guardian", "GUARDIAN · INTEGRIDADE", "GUARDIAN · INTEGRITY");
            A("ow_ordnance", "ORDNANCE · RECURSOS", "ORDNANCE · RESOURCES");
            A("ow_phantom", "PHANTOM · RECONHECIMENTO", "PHANTOM · RECON");
            A("ow_god", "Invulnerável", "Invulnerable");
            A("ow_heal", "Restaurar vida", "Restore health");
            A("ow_telemetry", "Telemetria na tela", "On-screen telemetry");
            A("ow_ammo", "Munição infinita", "Infinite ammo");
            A("ow_knife", "Faca indestrutível", "Unbreakable knife");
            A("ow_arsenal", "Arsenal completo", "Full arsenal");
            A("ow_cycle", "Trocar arma", "Cycle weapon");
            A("ow_time", "Velocidade do tempo: {0:0.00}x", "Time speed: {0:0.00}x");
            A("ow_freeze", "Congelar inimigos", "Freeze enemies");
            A("ow_reset", "Devolver inimigos à origem", "Send enemies back home");
            A("ow_teleport", "Teletransportar para:", "Teleport to:");
            A("ow_note", "Uso de teste: a partida fica sem classificação.", "Test use: the run gets no rank.");
        }
    }
}
