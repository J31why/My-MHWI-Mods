

namespace AutoItemRestore;

internal static class FoodSkillDic
{
    private static readonly Dictionary<int, (string Cn, string En)> _dic = new()
        {
            { -1, ("无",           "None") },
            { 0,  ("猫的研磨术",           "Felyne Polisher") },
            { 1,  ("猫的骑乘好手",         "Felyne Rider") },
            { 2,  ("猫的火场怪力",         "Felyne Heroics") },
            { 3,  ("猫的解体术【大】",     "Felyne Carver (Hi)") },
            { 4,  ("猫的解体术【小】",     "Felyne Carver (Lo)") },
            { 5,  ("猫的医疗术",           "Felyne Medic") },
            { 6,  ("猫的体术",             "Felyne Black Belt") },
            { 7,  ("猫的火药术",           "Felyne Pyro") },
            { 8,  ("猫的特殊攻击术",       "Felyne Specialist") },
            { 9,  ("猫的防御术【大】",     "Felyne Defender (Hi)") },
            { 10, ("猫的防御术【小】",     "Felyne Defender (Lo)") },
            { 11, ("猫的收获祭",           "Felyne Harvester") },
            { 12, ("猫的射击术",           "Felyne Sharpshooter") },
            { 13, ("招财猫的幸运",         "Lucky Cat") },
            { 14, ("猫的弹武好手",         "Felyne Deflector") },
            { 15, ("猫的逃走术",           "Felyne Escape Artist") },
            { 16, ("猫的宅配术",           "Felyne Sprinter") },
            { 17, ("猫的搬运铁人",         "Felynebacker") },
            { 18, ("猫的弱小招来!",        "Felyne Weakener") },
            { 19, ("猫的换算术",           "Felyne Exchanger") },
            { 20, ("猫的起身术【大】",     "Felyne Riser (Hi)") },
            { 21, ("猫的起身术【小】",     "Felyne Riser (Lo)") },
            { 22, ("猫的粗暴射击",         "Felyne Temper") },
            { 23, ("猫的攀爬藤蔓达人",     "Felyne Cliffhanger") },
            { 24, ("猫的紧抓不放铁人",     "Felyne Gripper") },
            { 25, ("猫的解体铁人",         "Felyne Iron Carver") },
            { 26, ("猫的着地术",           "Felyne Lander") },
            { 27, ("猫的蛮干术",           "Felyne Bulldozer") },
            { 28, ("猫的赠品术",           "Felyne Foodie") },
            { 29, ("猫的击晕术",           "Felyne Slugger") },
            { 30, ("招财猫的财运",         "Felyne Fat Cat") },
            { 31, ("猫的炮击术",           "Felyne Bombardier") },
            { 32, ("猫的大毅力",           "Felyne Moxie") },
            { 33, ("猫的异臭球达人",       "Felyne Dungmaster") },
            { 34, ("猫的理毛好手",         "Felyne Groomer") },
            { 35, ("猫的长靴术",           "Felyne Fur Coating") },
            { 36, ("猫的受身术",           "Felyne Acrobat") },
            { 37, ("Unavailable",          "Felyne Gamechanger") },
            { 38, ("猫的随从指导术",       "Felyne Trainer") },
            { 39, ("猫的短期催眠术",       "Felyne Booster") },
            { 40, ("猫的坚定不移术",       "Felyne Feet") },
            { 41, ("猫的钓鱼好手",         "Felyne Fisher") },
            { 42, ("猫的休息术",           "Cool Cat") },
            { 43, ("猫的报酬金保险",       "Felyne Insurance") },
            { 44, ("猫的吸引仇恨好手",     "Felyne Provoker") },
            { 45, ("猫的后面交给你们了!",  "Felyne Parting Gift") },
            { 46, ("猫的探索好天气",       "Felyne Researcher") },
            { 47, ("猫的天气预报",         "Felyne Weathercat") },
            { 48, ("猫的防滑鞋",           "Felyne Cleats") },
            { 49, ("猫的裁缝术",           "Felyne Tailor") },
            { 50, ("猫的生命保险",         "Felyne Safeguard") },
            { 51, ("猫的园艺术",           "Felyne Gardener") },
            { 52, ("猫的残余术",           "Felyne Scavenger") },
            { 53, ("猫的生物博士",         "Felyne Zoomaster") },
            { 54, ("猫的生物学者",         "Felyne Biologist") },
            { 55, ("猫的大型吸引",         "Felyne Macrozoologist") },
            { 56, ("猫的小型吸引",         "Felyne Microzoologist") },
        };
    public static string GetName(int id, bool isChinese)
    {
        if(_dic.TryGetValue(id,out var name))
            return isChinese ? name.Cn : name.En;
        return string.Empty;
    }
}
