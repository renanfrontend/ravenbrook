namespace Ravenbrook
{
    /// <summary>Documentos espalhados pela delegacia (português e inglês; os outros idiomas leem em inglês).</summary>
    public static class Documents
    {
        public const int Count = 4;

        static readonly string[] TitlePt =
        {
            "Registro de ocorrência nº 4471",
            "Diário do chefe Albrecht",
            "Memorando interno — Helix Biotech",
            "Bilhete amassado",
        };

        static readonly string[] TitleEn =
        {
            "Incident report #4471",
            "Chief Albrecht's diary",
            "Internal memo — Helix Biotech",
            "Crumpled note",
        };

        static readonly string[] BodyPt =
        {
            "21 de setembro, 23h40.\n\nChamado no bairro de Marrow Hill: homem atacando vizinhos a mordidas. Ao chegar, a viatura 12 encontrou o suspeito comendo a própria vítima. Foram necessários seis disparos para contê-lo.\n\nO legista diz que o coração dele já tinha parado horas antes da ocorrência.\n\nSão onze casos iguais nesta semana. Pedi reforço ao condado. Ninguém respondeu.",
            "24 de setembro.\n\nA cidade caiu. Mandei trancar as alas: a porta oeste ficou com a Chave do Corvo, a biblioteca com a Chave do Leão. Guardei a do Leão aqui no meu escritório.\n\nOs medalhões da estátua estão espalhados: um na biblioteca, um nos registros e o outro fica comigo. Sem os três, o portão da garagem não abre. Foi ideia minha. Que ironia.\n\nA Helix mandou \"algo\" para limpar as provas. Enorme, de sobretudo e máscara de gás. Não sente dor e não para. Só não entra onde há a máquina de escrever. Não sei por quê.",
            "CONFIDENCIAL — DIVISÃO DE CONTENÇÃO\n\nO vazamento do Vírus-V no laboratório subterrâneo de Ravenbrook ultrapassou o nível 4. A taxa de infecção na população é estimada em 92%.\n\nProtocolo Overwatch autorizado: liberar a unidade B.R.T.-03 para recuperar amostras e eliminar testemunhas. A delegacia é prioridade: o chefe Albrecht sabe demais.\n\nNenhum funcionário deve deixar a cidade sem autorização da diretoria.",
            "Se alguém ler isto: a garagem é a única saída. O portão está ligado à estátua do saguão e precisa de três medalhões.\n\nA Chave do Corvo ficou no escritório leste, em cima da mesa perto da janela. Eu não consegui voltar lá.\n\nUse a máquina de escrever para registrar seu progresso e o baú para guardar o que não couber no bolso.\n\nE não confie nos que estão deitados no chão.",
        };

        static readonly string[] BodyEn =
        {
            "September 21, 11:40 PM.\n\nCall from the Marrow Hill district: a man biting his neighbours. Unit 12 found the suspect eating his own victim. It took six shots to stop him.\n\nThe coroner says his heart had stopped hours before the incident.\n\nThat makes eleven identical cases this week. I asked the county for backup. No one answered.",
            "September 24.\n\nThe city has fallen. I had the wings locked: the west door takes the Crow key, the library takes the Lion key. I kept the Lion key here in my office.\n\nThe statue's medallions are scattered: one in the library, one in the records room, and I keep the last one. Without all three, the garage gate won't open. It was my idea. How ironic.\n\nHelix sent \"something\" to clean up the evidence. Huge, in a trench coat and a gas mask. It feels no pain and never stops. It just won't enter rooms with a typewriter. I don't know why.",
            "CONFIDENTIAL — CONTAINMENT DIVISION\n\nThe V-Virus leak at the Ravenbrook underground lab has exceeded level 4. The infection rate in the population is estimated at 92%.\n\nOverwatch protocol authorised: release unit B.R.T.-03 to recover samples and eliminate witnesses. The police station is the priority: Chief Albrecht knows too much.\n\nNo employee may leave the city without board approval.",
            "If anyone reads this: the garage is the only way out. The gate is linked to the statue in the main hall and needs three medallions.\n\nThe Crow key is in the east office, on the desk near the window. I couldn't make it back there.\n\nUse the typewriter to record your progress and the box to store what doesn't fit in your pockets.\n\nAnd don't trust the ones lying on the floor.",
        };

        public static string Title(int i) => Loc.IsPt ? TitlePt[i] : TitleEn[i];
        public static string Body(int i) => Loc.IsPt ? BodyPt[i] : BodyEn[i];
    }
}
