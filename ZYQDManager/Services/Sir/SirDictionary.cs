using System.Collections.Generic;

namespace ZYQDManager.Services.Sir;

/// <summary>
/// 检测项目字段模板字典（从老系统 SIRDictionary 迁入）
/// </summary>
public class SirDictionary
{
       
        //基础版遥控器
        // 1. Specification映射字典：Key=Specification_type，Value=对应的Specification文本
        public readonly Dictionary<string, string> _specificationDescMap_remote = new Dictionary<string, string>
{
    {"1.1", "整机外观<br>Overall appearance of the product"},
    {"1.2", "LOGO<br>LOGO"},
    {"1.3", "标签铭牌<br>Label"},
    {"1.4", "尺寸<br>Size"},
    {"1.5", "LED/LCD显示<br>LED/LCD display"},
    {"1.6", "重量<br>Weight"},
    {"1.7", "螺钉<br>Screw"},
    {"2.1", "发射强度<br>Emission intensity"},
    {"2.2", "中心频率偏移<br>Carrier deviation"},
    {"2.3", "按键手感<br>Button feel"},
    {"2.4", "按键灵敏度<br>Button sensitivity"},
    {"2.5", "时间精度<br>Time accuracy only for timer remote"},
    {"2.6", "遥控距离<br>RF distance"},
    {"2.7", "充电电流<br>Charging current for chargeable"},
    {"2.8", "晃动测试<br>Shaking test"},
    {"2.9", "充电线插拔<br>Plug and unplug the charging cable for chargeable remote only."},
    {"2.10", "电池盖拆卸<br>Disassembly of battery cover"},
    {"3.1", "按键功能<br>Button function"},
    {"3.2", "显示内容<br>Display"},
    {"3.3", "发射器功能<br>Transmitter function"},
    {"3.4", "操作电机功能<br>Operate with motor"},
    {"4.1", "配件<br>Accessories"},
    {"1", "线路板外观"},
    {"2", "发射电流（mA）"},
    {"3", "待机电流"},
    {"4", "进入休眠时间"},
    {"5", "LED/LCD 显示"},
    {"6", "低电量提示"},
    {"7", "发射强度"},
    {"8", "中心频率偏移"},
    {"9", "充电电流"},
    {"10", "按键功能"},
    
};

        // 2. Criteria映射字典：Key=Specification_type，Value=对应的Criteria文本
        public readonly Dictionary<string, string> _criteriaMap_remote = new Dictionary<string, string>
{
    {"1.1", "整体外壳外观完好，色泽一致，无明显划伤、碰伤、敲击痕。<br>The overall appearance of the shell is intact, with consistent color and no obvious scratches, bumps, or impact marks."},
    {"1.2", "LOGO图标清晰，准确，无残缺。<br>The logo icon is clear, accurate, and without any defects."},
    {"1.3", "印刷清楚、正确，无明显倾斜、翘起现象。<br>The printing is clear and correct, with no obvious tilting or lifting."},
    {"1.4", "产品规格书<br>Refer to the product specification"},
    {"1.5", "显示清晰，亮度均匀<br>Clear display and uniform brightness"},
    {"1.6", "参照说明书<br>Refer to the product specification"},
    {"1.7", "不少打螺钉,每个螺钉都拧到位。<br>All screws are installed, and each one is properly tightened."},
    {"2.1", "10CM/≥-30DB"},
    {"2.2", "±100KHz"},
    {"2.3", "高度一致，手感一致，回弹有力，无卡死。<br>All buttons are uniform in height and feel, with strong rebound and no sticking."},
    {"2.4", "所有按键间隔1S钟连续按5次,功能都正常。<br>All buttons work correctly when pressed 5 times with a 1-second interval between each press."},
    {"2.5", "每天误差在±5S内,若无填NA<br>The daily error is within ±5S."},
    {"2.6", "室内≥35m<br>Indoor ≥ 35m"},
    {"2.7", "充电电流为1C<br>Charging current is 1C"},
    {"2.8", "手持样品进行摇晃5次，要求样品不能出现异响或者内部零件有窜动现象。<br>Shake the remote in hand 5 times.No abnormal noise or any movement of internal comonents."},
    {"2.9", "反复插拔20次，要求插座无松动。<br>Repeatedly plug and unplug 20 times, ensuring that the socket is not loose."},
    {"2.10", "反复拆装20次，要求电池盖/仓无晃动。<br>Disassemble and assemble the battery cover 20 times.No loose for the battery cover."},
    {"3.1", "所有按键功能正常<br>All the functions on each button work correctly."},
    {"3.2", "每一步操作的显示提示内容都正确。<br>The display is correct for every operation step."},
    {"3.3", "参照说明书<br>Refer to the specification"},
    {"3.4", "参照说明书<br>Refer to the specification"},
    {"4.1", "配件齐全，外观良好。<br>Complete accessories and good appearance."},
    {"1", "电路板焊接整齐、美观，元器件位置正确，线路板上无明显珠，焊渣。"},
    {"2", "参照说明书"},
    {"3", "≤15uA"},
    {"4", "≤10S"},
    {"5", "显示清晰，亮度均匀"},
    {"6", "供电电压2.7V以下出现低电量提示"},
    {"7", "10CM /≥-30DB:"},
    {"8", "±100KHz"},
    {"9", "充电电流为1C"},
    {"10", "所有按键功能正常"},
    
};
        //填写内容格式
        public readonly Dictionary<string, string> _fillingtype_remote = new Dictionary<string, string>
            {

                {"1.1", "2"},
                {"1.2", "2"},
                {"1.3", "2"},
                {"1.4", "2"},
                {"1.5", "2"},
                {"1.6", "0"},
                {"1.7", "2"},
                {"2.1", "0"},
                {"2.2", "1"},
                {"2.3", "1"},
                {"2.4", "1"},
                {"2.5", "2"},
                {"2.6", "0"},
                {"2.7", "2"},
                {"2.8", "2"},
                {"2.9", "2"},
                {"2.10", "2"},
                {"3.1", "1"},
                {"3.2", "1"},
                {"3.3", "1"},
                {"3.4", "1"},
                {"4.1", "1"},
                {"1", "1"},
                {"2", "0"},
                {"3", "0"},
                {"4", "1"},
                {"5", "1"},
                {"6", "1"},
                {"7", "0"},
                {"8", "1"},
                {"9", "1"},
                {"10", "1"}
            };




        /// <summary>
        /// 基础版电机
        /// </summary>

        public readonly Dictionary<string, string> _specificationDescMap_motor = new Dictionary<string, string>
{

    {"M.1", "指示灯<br>LED"},
    {"M.2", "行程头功能<br>Function of the motor head button"},
    {"M.3", "行程头按键功能<br>Motor head button function testing"},
    {"M.4", "添码 / 删码<br>Pairing/Delete pairing"},
    {"M.5", "行程设置<br>Limit setting"},
    {"M.6", "最佳运行位置<br>Favourite position"},
    {"M.7", "点动测试（1 圈）<br>One-touch mode (1 round)"},
    {"M.8", "换向<br>Direction Reversing"},
    {"M.9", "点动连动切换<br>One-touch/Contant-touch mode switching"},
    {"M.10", "速度切换<br>Speed adjusting"},
    {"M.11", "连续运行时间<br>Continuous running time"},
    {"M.12", "断电测试<br>Power off testing"},
    {"M.13", "联网功能<br>App connecting function"},
    {"M.14", "成品充电<br>Charging"},
    {"M.15", "接收灵敏度<br>RF signal sensitivity"},
    {"M.16", "客户特殊功能要求<br>Customized function testing"},
    {"M.17", "充电提示 / 唤醒<br>Charging indicate"},
    {"M.18", "成品空载转速<br>Motor speed without loading"},
    {"M.19", "成品长度 / 管长<br>Motor length/Tube length"},
    {"M.20", "电机空载噪音<br>Motor noise without loading"},
    {"M.21", "电机外观<br>Motor appearance"},
    {"M.22", "按键灵敏度<br>Key sensitivity"},
    {"M.23", "电机配件<br>Motor accessory"},
    {"M.24", "电源线<br>Power cable"},
    {"M.25", "电量检测（V）<br>Battery capacity testing（V）"},
    {"M.26", "半成品充电<br>Semi-finished charge"},
    {"M.27", "功耗（UA）<br>Power consumption（UA）"},
    {"M.28", "电机性能（带载）<br>Motor performance with loading"},
    {"M.29", "成品满载转速<br>Motor speed with full loading<br>样册上是空载转速，满载转速跟样册会有差异，此数据仅内部参考"},
    {"M.30", "电机组合<br>Motor assembly"},
    {"M.31", "成品晃动测试<br>Vibration Testing"}
};
        public readonly Dictionary<string, string> _criteriaMap_motor = new Dictionary<string, string>
{

    {"M.1", "参照产品规格书功能指示<br>Refer to product specification for functional instructions"},
    {"M.2", "按键，LED,充电，软排线 以上均正常连接<br>Buttons, LED, charging, soft cable are connected correctly."},
    {"M.3", "根据规格书每个功能能正常工作,且LED灯指示正确<br>Each function is tested.The LED display is correct."},
    {"M.4", "包括多发射器配码删码（参照规格书）<br>Test pairing and deleting pairing with multiple remotes(refer to the specification)."},
    {"M.5", "上下行程以及行程调整、行程删除三种（参照规格书）<br>Setting limits,adjusting and deleting limits(refer to the specification)."},
    {"M.6", "设置、删除、运行三种情况（参照规格书）<br>Setting, deleting, running to the favourite position(refer to the specification)"},
    {"M.7", "符合每次按键电机响应并动作<br>The motor responds and operates by every pressing."},
    {"M.8", "有无行程分两种操作（参照规格书）<br>With limits and without limits (refer to the specification)"},
    {"M.9", "参照规格书操作<br>Operate according to specifications"},
    {"M.10", "参照规格书操作<br>Operate according to specifications"},
    {"M.11", "默认6min<br>Default 6min"},
    {"M.12", "断电上电后检查行程点<br>Check the limits after power off and power on"},
    {"M.13", "部分电机有该功能（参考规格书）,若无填NA<br>Only for the motors with this function(refer to the specification)."},
    {"M.14", "USB、Type-C口测试电流，LED灯情况<br>Test the charging current when charging,check the LED display."},
    {"M.15", "根据下发的灵敏度检验规范测试，包括发射器测试数值，以及室内室外拉距离，单控，群控<br>Test the RF signal distance under channel control and group control indoor and outdoor."},
    {"M.16", "请备注具体功能（模块，485干触点，齐平等）,若无填NA<br>Please note specific functions (module, 485, dry contacts,etc."},
    {"M.17", "参照规格书<br>Refer to the specification"},
    {"M.18", "参照规格书<br>Refer to the specification"},
    {"M.19", "机械工程师提供<br>By measure"},
    {"M.20", ""},
    {"M.21", "直观明显的外观问题，外观瑕疵<br>Visual obvious appearance defects."},
    {"M.22", "按键手感功能,若无填NA<br>Key feel function"},
    {"M.23", "参考样品需求,若无填NA<br>Refer to the sample requirement"},
    {"M.24", "参考样品需求<br>Refer to the sample requirement"},
    {"M.25", "电量检测（V）<br>Battery capacity testing（V）<br>电量需在70-80%<br>Battery capacity needs to be 70-80%"},
    {"M.26", "半成品充电<br>Semi-finished charge<br>测试充电电流，截止电压<br>Test charging current, cut-off voltage"},
    {"M.27", "功耗（UA）<br>Power consumption（UA）<br>静态≤120uA<br>Static ≤120uA"},
    {"M.28", "电机性能（带载）<br>Motor performance with loading<br>满负载不打滑，行程正常<br>Slipping does not happen with correct limits"},
    {"M.29", "成品满载转速<br>Motor speed with full loading<br>参照规格书<br>Reference specification<br>样册上是空载转速，满载转速跟样册会有差异，此数据仅内部参考"},
    {"M.30", "电机组合<br>Motor assembly<br>参考样品需求<br>Reference sample requirement"},
    {"M.31", "成品晃动测试<br>Vibration Testing<br>制作震动工装或简易震动设备购买<br>Make vibration tooling or simple vibration equipment purchase."}
};
        // 样品部件字典
        public readonly Dictionary<string, string> _sampleComponents_motor = new Dictionary<string, string>
{
    {"S.1", "整机"},
    {"S.2", "部件1"},
    {"S.3", "部件2"},
    {"S.4", "部件3"},
    {"S.5", "部件4"},
    {"S.6", "部件5"},
    {"S.7", "部件6"}
};
        // 要求字典
        public readonly Dictionary<string, string> _requirements_motor = new Dictionary<string, string>
{
    {"S.1", ""},
    {"S.2", ""},
    {"S.3", ""},
    {"S.4", ""},
    {"S.5", ""},
    {"S.6", ""},
    {"S.7", ""}
};
        //填写内容格式
        public readonly Dictionary<string, string> _fillingtype_motor = new Dictionary<string, string>
        {
            {"M.1", "1"},
            {"M.2", "1"},
            {"M.3", "1"},
            {"M.4", "1"},
            {"M.5", "1"},
            {"M.6", "1"},
            {"M.7", "1"},
            {"M.8", "1"},
            {"M.9", "1"},
            {"M.10", "1"},
            {"M.11", "0"},
            {"M.12", "1"},
            {"M.13", "1"},
            {"M.14", "1"},
            {"M.15", "1"},
            {"M.16", "0"},
            {"M.17", "1"},
            {"M.18", "0"},
            {"M.19", "1"},
            {"M.20", "0"},
            {"M.21", "1"},
            {"M.22", "1"},
            {"M.23", "0"},
            {"M.24", "0"},
            {"M.25", "0"},
            {"M.26", "0"},
            {"M.27", "0"},
            {"M.28", "0"},
            {"M.29", "0"},
            {"M.30", "0"},
            {"M.31", "0"},
        };


        /// <summary>
        /// JCA-内置RF控制机械行程交流管状电机
        /// </summary>
        public readonly Dictionary<string, string> _specificationDescMap_JCA_in_RF_Mech = new Dictionary<string, string>
            {
                  {"M.1", "额定电压<br>Rated voltage"},
                {"M.2", "频率<br>Frequency"},
                {"M.3", "指示灯<br>LED"},
                {"M.4", "按键功能<br>Motor head button function testing"},
                {"M.5", "添码/删码<br>Pairing/Delete pairing"},
                {"M.6", "点动测试（1圈）<br>One-touch mode (1 round)"},
                {"M.7", "换向<br>Direction Reversing"},
                {"M.8", "点动连动切换<br>One-touch/Contant-touch mode switching"},
                {"M.9", "断电测试<br>Power off testing"},
                {"M.10", "接收灵敏度<br>RF signal sensitivity"},
                {"M.11", "客户特殊功能要求<br>Customized function testing"},
                {"M.12", "满载刹车性能<br>Full load braking performance"},
                {"M.13", "成品空载转速<br>Motor speed without loading"},
                {"M.14", "空载噪音<br>Motor noise without loading"},
                {"M.15", "热保护时间<br>Thermal protection time"},
                {"M.16", "限位调节<br>Limit adjustment"},
                {"M.17", "行程齿圈检测<br>Travel ring gear inspection"},
                {"M.18", "成品长度/管长<br>Motor length/Tube length"},
                {"M.19", "电机外观<br>Motor appearance"},
                {"M.20", "电机配件<br>Motor accessory"},
                {"M.21", "电源线长度、规格<br>Power Cable length and specification"},
                {"M.22", "低电压运行<br>Low voltage operation"},
                {"M.23", "满载上行转速<br>Full load up speed"},
                {"M.24", "满载下行转速<br>Full load down speed"},
                {"M.25", "成品晃动测试<br>Vibration Testing"},
                {"M.26", "电机性能（带载）<br>Motor performance with loading"},
                {"M.27", "成品满载转速<br>Motor speed with full loading<br>样册上是空载转速，满载转速跟样册会有差异，此数据仅内部参考"},
                {"M.28", "定子参数或料号<br>Stator parameters or part number"},
                {"M.29", "减速箱型号或料号<br>Gearbox model or part number"}
            };
        public readonly Dictionary<string, string> _criteriaMap_JCA_in_RF_Mech = new Dictionary<string, string>
{
    {"M.1", "参照产品规格书定义<br>Refer to the definition in the product specification sheet"},
    {"M.2", "参照产品规格书定义<br>Refer to the definition in the product specification sheet"},
    {"M.3", "参照产品规格书功能指示（无LED，则填无）<br>Refer to product specification for functional instructions"},
    {"M.4", "根据规格书每个功能能正常工作,且LED灯指示正确（无按键，则填无）<br>Each function is tested.The LED display is correct."},
    {"M.5", "包括多发射器配码删码（参照规格书）<br>Test pairing and deleting pairing with multiple remotes(refer to the specification)."},
    {"M.6", "符合每次按键电机响应并动作<br>The motor responds and operates by every pressing."},
    {"M.7", "有无行程分两种操作（参照规格书）<br>With limits and without limits (refer to the specification)"},
    {"M.8", "参照规格书操作<br>Operate according to specification"},
    {"M.9", "断电上电后检查行程点<br>Check the limits after power off and power on"},
    {"M.10", "根据下发的灵敏度检验规范测试，包括发射器测试数值，以及室内室外拉距离，单控，群控<br>Test the RF signal distance under channel control and group control indoor and outdoor."},
    {"M.11", "请备注具体功能（五芯强电功能等）<br>Please specify the specific functions (five core strong current function, etc.)"},
    {"M.12", "满负载向下、向上运行时连续停止5次，电机无打滑现象<br>Stop continuously 5 times when running downwards and upwards at full load, and the motor does not slip"},
    {"M.13", "参照规格书<br>Refer to the specification"},
    {"M.14", "参照参数表QC-000634<br>Refer to the parameter table QC-000634"},
    {"M.15", "参照参数表QC-000634<br>Refer to the parameter table QC-000634"},
    {"M.16", "旋转上、下限位调节蜗杆，根据行程头上指示方向，能够使电机停止或运行，且旋转蜗杆时转动顺畅，无卡顿现象<br>Rotate the upper and lower limit adjustment worm, according to the direction indicated on the travel head, it can stop or run the motor, and rotate the worm smoothly without any jamming phenomenon"},
    {"M.17", "转动行程齿圈能够使电机停止或运行，且旋转时转动顺畅，无卡顿现象<br>Rotating the travel ring gear can stop or operate the motor, and the rotation is smooth without any jamming phenomenon"},
    {"M.18", "填写整机长度<br>Fill in the overall length of the motor."},
    {"M.19", "直观明显的外观问题，外观瑕疵<br>Visual obvious appearance defects."},
    {"M.20", "参考样品需求，若无配件，则填无；若有配件，则填写配件编号<br>Refer to the sample requirements, if there is no accessory, fill in 'none'; Otherwise, fill in the accessory number"},
    {"M.21", "参考样品需求，若无电源线，则填无；若有电源线，则需填写电源线长度、规格<br>Refer to the sample requirements, if there is no power cable, fill in 'none'; otherwise, fill in the length and specifications of the cable"},
    {"M.22", "参照参数表QC-000634<br>Refer to the parameter table QC-000634"},
    {"M.23", "参照参数表QC-000634<br>Refer to the parameter table QC-000634"},
    {"M.24", "参照电机转速标注<br>Refer to the motor speed annotation"},
    {"M.25", "成品晃动测试<br>Vibration Testing<br>制作震动工装或简易震动设备购买<br>Make vibration tooling or simple vibration equipment purchase."},
    {"M.26", "电机性能（带载）<br>Motor performance with loading<br>满负载不打滑，行程正常<br>Slipping does not happen with correct limits"},
    {"M.27", "成品满载转速<br>Motor speed with full loading<br>参照规格书<br>Reference specification<br>样册上是空载转速，满载转速跟样册会有差异，此数据仅内部参考"},
    {"M.28", "未受控产品请填写定子绕线参数，已受控产品请填写对应料号<br>For uncontrolled products, please fill in the stator winding parameters. For controlled products, please fill in the corresponding part number"},
    {"M.29", "未受控产品请填写定子绕线参数，已受控产品请填写对应料号<br>For uncontrolled products, please fill in the stator winding parameters. For controlled products, please fill in the corresponding part number"}
};
        // 样品部件字典
        public readonly Dictionary<string, string> _sampleComponents_JCA_in_RF_Mech = new Dictionary<string, string>
{
    {"S.1", "行程组件"},
    {"S.2", "主控PCB板"},
    {"S.3", "马达组合"},
    {"S.4", "电容"},
    {"S.5", "减速箱"},
    {"S.6", "电源线"},
    {"S.7", "整机"}
};
        // 要求字典
        public readonly Dictionary<string, string> _requirements_JCA_in_RF_Mech = new Dictionary<string, string>
{
    {"S.1", "行程组件 图片能够体现蜗杆颜色、安装方向，丝杆情况，行程头型号"},
    {"S.2", "主控PCB板 正反面留底"},
    {"S.3", "马达组合 图片能够体现定子线材颜色  正反面留底"},
    {"S.4", "电容 电容容量、铭牌标贴  正反面留底"},
    {"S.5", "减速箱 体现减速箱上型号，输出轴类型"},
    {"S.6", "电源线 供方、芯线数、认证、规格留底"},
    {"S.7", "整机 行程头端、减速端、整机、铭牌、二维码等角度留底"}
};
        //填写内容格式
        public readonly Dictionary<string, string> _fillingtype_JCA_in_RF_Mech = new Dictionary<string, string>
{
    {"M.1", "0"},
    {"M.2", "0"},
    {"M.3", "1"},
    {"M.4", "1"},
    {"M.5", "1"},
    {"M.6", "1"},
    {"M.7", "1"},
    {"M.8", "1"},
    {"M.9", "1"},
    {"M.10", "0"},
    {"M.11", "0"},
    {"M.12", "1"},
    {"M.13", "0"},
    {"M.14", "0"},
    {"M.15", "0"},
    {"M.16", "1"},
    {"M.17", "1"},
    {"M.18", "0"},
    {"M.19", "0"},
    {"M.20", "0"},
    {"M.21", "0"},
    {"M.22", "1"},
    {"M.23", "0"},
    {"M.24", "0"},
    {"M.25", "1"},
    {"M.26", "1"},
    {"M.27", "0"},
    {"M.28", "0"},
    {"M.29", "0"},
    
};

        /// <summary>
        /// JCA-内置RF控制电子行程交流管状电机
        /// </summary>
        public readonly Dictionary<string, string> _specificationDescMap_JCA_in_RF_Elec= new Dictionary<string, string>
{
    {"M.1", "额定电压<br>Rated voltage"},
    {"M.2", "频率<br>Frequency"},
    {"M.3", "指示灯<br>LED"},
    {"M.4", "按键功能<br>Motor head button function testing"},
    {"M.5", "添码/删码<br>Pairing/Delete pairing"},
    {"M.6", "行程设置<br>Limit setting"},
    {"M.7", "点动测试（1圈）<br>One-touch mode (1 round)"},
    {"M.8", "换向<br>Direction Reversing"},
    {"M.9", "点动连动切换<br>One-touch/Contant-touch mode switching"},
    {"M.10", "断电测试<br>Power off testing"},
    {"M.11", "联网功能<br>App connecting function"},
    {"M.12", "接收灵敏度<br>RF signal sensitivity"},
    {"M.13", "客户特殊功能要求<br>Customized function testing"},
    {"M.14", "成品空载转速<br>Motor speed without loading"},
    {"M.15", "满载刹车性能<br>Full load braking performance"},
    {"M.16", "电机空载噪音<br>Motor noise without loading"},
    {"M.17", "热保护时间<br>Thermal protection time"},
    {"M.18", "成品长度/管长<br>Motor length/Tube length"},
    {"M.19", "电机外观<br>Motor appearance"},
    {"M.20", "电机配件<br>Motor accessory"},
    {"M.21", "电源线长度、规格<br>Power Cable length and specification"},
    {"M.22", "遇阻功能检测<br>Obstacle detection function"},
    {"M.23", "满载上行转速<br>Full load up speed"},
    {"M.24", "满载下行转速<br>Full load down speed"},
    {"M.25", "低电压运行<br>Low voltage operation"},
    {"M.26", "成品晃动测试<br>Vibration Testing"},
    {"M.27", "定子参数或料号<br>Stator parameters or part number"},
    {"M.28", "减速箱型号或料号<br>Gearbox model or part number"}
};
        public readonly Dictionary<string, string> _criteriaMap_JCA_in_RF_Elec = new Dictionary<string, string>
{
    {"M.1", "参照产品规格书定义<br>Refer to the definition in the product specification sheet"},
    {"M.2", "参照产品规格书定义<br>Refer to the definition in the product specification sheet"},
    {"M.3", "参照产品规格书功能指示（无LED，则填无）<br>Refer to product specification for functional instructions"},
    {"M.4", "根据规格书每个功能能正常工作,且LED灯指示正确（无按键，则填无）<br>Each function is tested.The LED display is correct."},
    {"M.5", "包括多发射器配码删码（参照规格书）<br>Test pairing and deleting pairing with multiple remotes(refer to the specification)."},
    {"M.6", "上下行程以及行程调整、行程删除三种（参照规格书）<br>Setting limits,adjusting and deleting limits(refer to the specification)."},
    {"M.7", "符合每次按键电机响应并动作<br>The motor responds and operates by every pressing."},
    {"M.8", "有无行程分两种操作（参照规格书）<br>With limits and without limits (refer to the specification)"},
    {"M.9", "参照规格书操作<br>Operate according to specification"},
    {"M.10", "断电上电后检查行程点<br>Check the limits after power off and power on"},
    {"M.11", "部分电机有该功能（参考规格书）<br>Only for the motors with this function(refer to the specification)."},
    {"M.12", "根据下发的灵敏度检验规范测试，包括发射器测试数值，以及室内室外拉距离，单控，群控<br>Test the RF signal distance under channel control and group control indoor and outdoor."},
    {"M.13", "请备注具体功能（模块，485干触点，五芯强电等）<br>Please note specific functions (module,485dry contacts,five core strong current,etc function, etc.)"},
    {"M.14", "参照规格书<br>Refer to the specification"},
    {"M.15", "满负载向下、向上运行时连续停止5次，电机无打滑现象<br>Stop continuously 5 times when running downwards and upwards at full load, and the motor does not slip"},
    {"M.16", "参照参数表QC-000634<br>Refer to the parameter table QC-000634"},
    {"M.17", "参照参数表QC-000634<br>Refer to the parameter table QC-000634"},
    {"M.18", "填写整机长度<br>Fill in the overall length of the motor."},
    {"M.19", "直观明显的外观问题，外观瑕疵<br>Visual obvious appearance defects."},
    {"M.20", "参考样品需求，若无配件，则填无；若有配件，则填写配件编号<br>Refer to the sample requirements, if there is no accessory, fill in 'none'; Otherwise, fill in the accessory number"},
    {"M.21", "参考样品需求，若无电源线，则填无；若有电源线，则需填写电源线长度、规格<br>Refer to the sample requirements, if there is no power cable, fill in 'none'; otherwise, fill in the length and specifications of the cable"},
    {"M.22", "参照规格书操作<br>Operate according to specification"},
    {"M.23", "参照参数表QC-000634<br>Refer to the parameter table QC-000634"},
    {"M.24", "参照电机转速标注<br>Refer to the motor speed annotation"},
    {"M.25", "参照参数表QC-000634<br>Refer to the parameter table QC-000634"},
    {"M.26", "成品晃动测试<br>Vibration Testing<br>制作震动工装或简易震动设备购买<br>Make vibration tooling or simple vibration equipment purchase."},
    {"M.27", "未受控产品请填写定子绕线参数，已受控产品请填写对应料号<br>For uncontrolled products, please fill in the stator winding parameters. For controlled products, please fill in the corresponding part number"},
    {"M.28", "未受控产品请填写定子绕线参数，已受控产品请填写对应料号<br>For uncontrolled products, please fill in the stator winding parameters. For controlled products, please fill in the corresponding part number"}
};
        // 样品部件字典
        public readonly Dictionary<string, string> _sampleComponents_JCA_in_RF_Elec = new Dictionary<string, string>
{
    {"S.1", "主控PCB板"},
    {"S.2", "马达组合"},
    {"S.3", "电容"},
    {"S.4", "减速箱"},
    {"S.5", "电源线"},
    {"S.6", "整机"}
};
        // 要求字典
        public readonly Dictionary<string, string> _requirements_JCA_in_RF_Elec = new Dictionary<string, string>
{
    {"S.1", "主控PCB板 正反面留底"},
    {"S.2", "马达组合 图片能够体现定子线材颜色  正反面留底"},
    {"S.3", "电容 电容容量、铭牌标贴  正反面留底"},
    {"S.4", "减速箱 体现减速箱上型号，输出轴类型"},
    {"S.5", "电源线 供方、芯线数、认证、规格留底"},
    {"S.6", "整机 行程头端、减速端、整机、铭牌、二维码等角度留底"}
};
        //填写内容格式
        public readonly Dictionary<string, string> _fillingtype_JCA_in_RF_Elec = new Dictionary<string, string>
{
    {"M.1", "0"},
    {"M.2", "0"},
    {"M.3", "1"},
    {"M.4", "1"},
    {"M.5", "1"},
    {"M.6", "1"},
    {"M.7", "1"},
    {"M.8", "1"},
    {"M.9", "1"},
    {"M.10", "1"},
    {"M.11", "1"},
    {"M.12", "0"},
    {"M.13", "0"},
    {"M.14", "0"},
    {"M.15", "1"},
    {"M.16", "0"},
    {"M.17", "0"},
    {"M.18", "0"},
    {"M.19", "0"},
    {"M.20", "0"},
    {"M.21", "0"},
    {"M.22", "1"},
    {"M.23", "0"},
    {"M.24", "0"},
    {"M.25", "1"},
    {"M.26", "1"},
    {"M.27", "0"},
    {"M.28", "0"},
   
};

        /// <summary>
        /// JCA-无RF电子行程交流管状电机
        /// </summary>
        public readonly Dictionary<string, string> _specificationDescMap_JCA_no_RF_Elec = new Dictionary<string, string>
{
    {"M.1", "额定电压<br>Rated voltage"},
    {"M.2", "频率<br>Frequency"},
    {"M.3", "指示灯<br>LED"},
    {"M.4", "按键功能<br>Motor head button function testing"},
    {"M.5", "行程设置<br>Limit setting"},
    {"M.6", "点动测试（1圈）<br>One-touch mode (1 round)"},
    {"M.7", "换向<br>Direction Reversing"},
    {"M.8", "点动连动切换<br>One-touch/Contant-touch mode switching"},
    {"M.9", "断电测试<br>Power off testing"},
    {"M.10", "联网功能<br>App connecting function"},
    {"M.11", "客户特殊功能要求<br>Customized function testing"},
    {"M.12", "满载刹车性能<br>Full load braking performance"},
    {"M.13", "电机空载噪音<br>Motor noise without loading"},
    {"M.14", "热保护时间<br>Thermal protection time"},
    {"M.15", "成品长度/管长<br>Motor length/Tube length"},
    {"M.16", "电机外观<br>Motor appearance"},
    {"M.17", "电机配件<br>Motor accessory"},
    {"M.18", "电源线长度、规格<br>Power Cable length and specification"},
    {"M.19", "遇阻功能检测<br>Obstacle detection function"},
    {"M.20", "低电压运行<br>Low voltage operation"},
    {"M.21", "满载上行转速<br>Full load up speed"},
    {"M.22", "满载下行转速<br>Full load down speed"},
    {"M.23", "成品晃动测试<br>Vibration Testing"},
    {"M.24", "定子参数或料号<br>Stator parameters or part number"},
    {"M.25", "减速箱型号或料号<br>Gearbox model or part number"}
};
        public readonly Dictionary<string, string> _criteriaMap_JCA_no_RF_Elec = new Dictionary<string, string>
{
    {"M.1", "参照产品规格书定义<br>Refer to the definition in the product specification sheet"},
    {"M.2", "参照产品规格书定义<br>Refer to the definition in the product specification sheet"},
    {"M.3", "参照产品规格书功能指示（无LED，则填无）<br>Refer to product specification for functional instructions"},
    {"M.4", "根据规格书每个功能能正常工作,且LED灯指示正确（无按键，则填无）<br>Each function is tested.The LED display is correct."},
    {"M.5", "上下行程以及行程调整、行程删除三种（参照规格书）<br>Setting limits,adjusting and deleting limits(refer to the specification)."},
    {"M.6", "符合每次按键电机响应并动作<br>The motor responds and operates by every pressing."},
    {"M.7", "有无行程分两种操作（参照规格书）<br>With limits and without limits (refer to the specification)"},
    {"M.8", "参照规格书操作<br>Operate according to specification"},
    {"M.9", "断电上电后检查行程点<br>Check the limits after power off and power on"},
    {"M.10", "部分电机有该功能（参考规格书）<br>Only for the motors with this function(refer to the specification)."},
    {"M.11", "请备注具体功能（模块，485干触点，齐平等）<br>Please note specific functions (module, 485, dry contacts,etc."},
    {"M.12", "满负载向下、向上运行时连续停止5次，电机无打滑现象<br>Stop continuously 5 times when running downwards and upwards at full load, and the motor does not slip"},
    {"M.13", "参照参数表QC-000634<br>Refer to the parameter table QC-000634"},
    {"M.14", "参照参数表QC-000634<br>Refer to the parameter table QC-000634"},
    {"M.15", "填写整机长度<br>Fill in the overall length of the motor."},
    {"M.16", "直观明显的外观问题，外观瑕疵<br>Visual obvious appearance defects."},
    {"M.17", "参考样品需求，若无配件，则填无；若有配件，则填写配件编号<br>Refer to the sample requirements, if there is no accessory, fill in 'none'; Otherwise, fill in the accessory number"},
    {"M.18", "参考样品需求，若无电源线，则填无；若有电源线，则需填写电源线长度、规格<br>Refer to the sample requirements, if there is no power cable, fill in 'none'; otherwise, fill in the length and specifications of the cable"},
    {"M.19", "参照规格书操作<br>Operate according to specification"},
    {"M.20", "参照参数表QC-000634<br>Refer to the parameter table QC-000634"},
    {"M.21", "参照参数表QC-000634<br>Refer to the parameter table QC-000634"},
    {"M.22", "参照电机转速标注<br>Refer to the motor speed annotation"},
    {"M.23", "成品晃动测试<br>Vibration Testing<br>制作震动工装或简易震动设备购买<br>Make vibration tooling or simple vibration equipment purchase."},
    {"M.24", "未受控产品请填写定子绕线参数，已受控产品请填写对应料号<br>For uncontrolled products, please fill in the stator winding parameters. For controlled products, please fill in the corresponding part number"},
    {"M.25", "未受控产品请填写定子绕线参数，已受控产品请填写对应料号<br>For uncontrolled products, please fill in the stator winding parameters. For controlled products, please fill in the corresponding part number"}
};
        // 样品部件字典
        public readonly Dictionary<string, string> _sampleComponents_JCA_no_RF_Elec = new Dictionary<string, string>
{
    {"S.1", "主控PCB板"},
    {"S.2", "马达组合"},
    {"S.3", "电容"},
    {"S.4", "减速箱"},
    {"S.5", "电源线"},
    {"S.6", "整机"}
};
        // 要求字典
        public readonly Dictionary<string, string> _requirements_JCA_no_RF_Elec = new Dictionary<string, string>
{
    {"S.1", "主控PCB板 正反面留底"},
    {"S.2", "马达组合 图片能够体现定子线材颜色"},
    {"S.3", "电容 电容容量、铭牌标贴"},
    {"S.4", "减速箱 体现减速箱上型号，输出轴类型"},
    {"S.5", "电源线 供方、芯线数、认证、规格留底"},
    {"S.6", "整机 行程头端、减速端、整机、铭牌、二维码等角度留底"}
};
        //填写内容格式
        public readonly Dictionary<string, string> _fillingtype_JCA_no_RF_Elec = new Dictionary<string, string>
{
    {"M.1", "0"},
    {"M.2", "0"},
    {"M.3", "1"},
    {"M.4", "1"},
    {"M.5", "1"},
    {"M.6", "1"},
    {"M.7", "1"},
    {"M.8", "1"},
    {"M.9", "1"},
    {"M.10", "1"},
    {"M.11", "0"},
    {"M.12", "1"},
    {"M.13", "0"},
    {"M.14", "0"},
    {"M.15", "0"},
    {"M.16", "1"},
    {"M.17", "0"},
    {"M.18", "0"},
    {"M.19", "1"},
    {"M.20", "1"},
    {"M.21", "0"},
    {"M.22", "0"},
    {"M.23", "1"},
    {"M.24", "0"},
    {"M.25", "0"},
    
};



        /// <summary>
        /// JCA-无RF机械行程交流管状电机
        /// </summary>
        public readonly Dictionary<string, string> _specificationDescMap_JCA_no_RF_Mech = new Dictionary<string, string>
{
    {"M.1", "额定电压<br>Rated voltage"},
    {"M.2", "频率<br>Frequency"},
    {"M.3", "满载刹车性能<br>Full load braking performance"},
    {"M.4", "成品空载转速<br>Motor speed without loading"},
    {"M.5", "空载噪音<br>No load noise"},
    {"M.6", "热保护时间<br>Thermal protection time"},
    {"M.7", "限位调节<br>Limit adjustment"},
    {"M.8", "行程齿圈检测<br>Travel ring gear inspection"},
    {"M.9", "成品长度/管长<br>Motor length/Tube length"},
    {"M.10", "电机外观<br>Motor appearance"},
    {"M.11", "电机配件<br>Motor accessory"},
    {"M.12", "电源线长度、规格<br>Cable length and specifications"},
    {"M.13", "满载上行转速<br>Full load up speed"},
    {"M.14", "满载下行转速<br>Full load down speed"},
    {"M.15", "低电压运行<br>Low voltage operation"},
    {"M.16", "成品晃动测试<br>Vibration Testing"},
    {"M.17", "定子参数或料号<br>Stator parameters or part number"},
    {"M.18", "减速箱型号或料号<br>Gearbox model or part number"}
};
        public readonly Dictionary<string, string> _criteriaMap_JCA_no_RF_Mech = new Dictionary<string, string>
{
    {"M.1", "参照产品规格书定义<br>Refer to the definition in the product specification sheet"},
    {"M.2", "参照产品规格书定义<br>Refer to the definition in the product specification sheet"},
    {"M.3", "满负载向下、向上运行时连续停止5次，电机无打滑现象<br>Stop continuously 5 times when running downwards and upwards at full load, and the motor does not slip"},
    {"M.4", "参照规格书<br>Refer to the specification"},
    {"M.5", "参照参数表QC-000634<br>Refer to the parameter table QC-000634"},
    {"M.6", "参照参数表QC-000634<br>Refer to the parameter table QC-000634"},
    {"M.7", "旋转上、下限位调节蜗杆，根据行程头上指示方向，能够使电机停止或运行，且旋转蜗杆时转动顺畅，无卡顿现象<br>Rotate the upper and lower limit adjustment worm, according to the direction indicated on the travel head, it can stop or run the motor, and rotate the worm smoothly without any jamming phenomenon"},
    {"M.8", "转动行程齿圈能够使电机停止或运行，且旋转时转动顺畅，无卡顿现象<br>Rotating the travel ring gear can stop or operate the motor, and the rotation is smooth without any jamming phenomenon"},
    {"M.9", "填写整机长度，若为拉杆或手柄电机，则拉杆或手柄长度需填写<br>Fill in the overall length of the motor. If it is a pull rod or handle motor, the length of the pull rod or handle needs to be filled in"},
    {"M.10", "直观明显的外观问题，外观瑕疵<br>Visual obvious appearance defects."},
    {"M.11", "参考样品需求，若无配件，则填无；若有配件，则填写配件编号<br>Reference sample requirements, if there are no accessories, fill in 'none'; If there are accessories, fill in the accessory number"},
    {"M.12", "参考样品需求，若无电源线，则填无；若有电源线，则需填写电源线长度、规格<br>Reference sample requirements, if there is no pcable, fill in 'none'; If there is a cable, the length and specifications of the cable needs to be filled in"},
    {"M.13", "参照参数表QC-000634<br>Refer to the parameter table QC-000634"},
    {"M.14", "参照电机转速标注<br>Refer to the motor speed annotation"},
    {"M.15", "参照参数表QC-000634<br>Refer to the parameter table QC-000634"},
    {"M.16", "成品晃动测试<br>Vibration Testing<br>制作震动工装或简易震动设备购买<br>Make vibration tooling or simple vibration equipment purchase."},
    {"M.17", "未受控产品请填写定子绕线参数，已受控产品请填写对应料号<br>For uncontrolled products, please fill in the stator winding parameters. For controlled products, please fill in the corresponding part number"},
    {"M.18", "未受控产品请填写定子绕线参数，已受控产品请填写对应料号<br>For uncontrolled products, please fill in the stator winding parameters. For controlled products, please fill in the corresponding part number"}
};
        // 样品部件字典
        public readonly Dictionary<string, string> _sampleComponents_JCA_no_RF_Mech = new Dictionary<string, string>
{
    {"S.1", "行程组件"},
    {"S.2", "马达组合"},
    {"S.3", "电容"},
    {"S.4", "减速箱"},
    {"S.5", "电源线"},
    {"S.6", "整机"}
};
        // 要求字典
        public readonly Dictionary<string, string> _requirements_JCA_no_RF_Mech = new Dictionary<string, string>
{
    {"S.1", "图片能够体现蜗杆颜色、安装方向，丝杆情况，行程头型号"},
    {"S.2", "图片能够体现定子线材颜色  正反面留底"},
    {"S.3", "电容容量、铭牌标贴  正反面留底"},
    {"S.4", "体现减速箱上型号，输出轴类型"},
    {"S.5", "供方、芯线数、认证、规格留底"},
    {"S.6", "行程头端、减速端、整机、铭牌、二维码等角度留底"}
};
        //填写内容格式
        public readonly Dictionary<string, string> _fillingtype_JCA_no_RF_Mech = new Dictionary<string, string>
{
    {"M.1", "0"},
    {"M.2", "0"},
    {"M.3", "1"},
    {"M.4", "0"},
    {"M.5", "0"},
    {"M.6", "0"},
    {"M.7", "1"},
    {"M.8", "1"},
    {"M.9", "0"},
    {"M.10", "0"},
    {"M.11", "0"},
    {"M.12", "0"},
    {"M.13", "0"},
    {"M.14", "0"},
    {"M.15", "1"},
    {"M.16", "1"},
    {"M.17", "0"},
    {"M.18", "0"},
    
};


        /// <summary>
        /// JCC-锂电池款开合帘电机
        /// </summary>
        public readonly Dictionary<string, string> _specificationDescMap_JCC_Li_Batt = new Dictionary<string, string>
{
    {"M.1", "指示灯<br>LED"},
    {"M.2", "按键功能<br>Motor button function testing"},
    {"M.3", "添码/删码<br>Pairing/Delete pairing"},
    {"M.4", "行程设置<br>Limit setting"},
    {"M.5", "最佳运行位置<br>Favourite position"},
    {"M.6", "换向<br>Direction Reversing"},
    {"M.7", "点动连动切换<br>One-touch/Contant-touch mode switching"},
    {"M.8", "速度切换<br>Speed adjusting"},
    {"M.9", "连续运行时间<br>Continuous running time"},
    {"M.10", "断电测试<br>Power off testing"},
    {"M.11", "联网功能<br>App connecting function"},
    {"M.12", "成品充电<br>Charging"},
    {"M.13", "接收灵敏度<br>RF signal sensitivity"},
    {"M.14", "客户特殊功能要求<br>Customized function testing"},
    {"M.15", "充电提示/唤醒<br>Charging indicate"},
    {"M.16", "成品空载转速<br>Motor speed without loading"},
    {"M.17", "成品长度/管长<br>Motor length/Tube length"},
    {"M.18", "电机空载噪音<br>Motor noise without loading"},
    {"M.19", "电机外观<br>Motor appearance"},
    {"M.20", "按键灵敏度<br>Key sensitivity"},
    {"M.21", "电机配件<br>Motor accessory"},
    {"M.22", "天线<br>Antenna"},
    {"M.23", "电源线长度、规格<br>Power Cable length and specification"},
    {"M.24", "电量检测（V）<br>Battery capacity testing（V）"},
    {"M.25", "半成品充电<br>Semi-finished charge"},
    {"M.26", "功耗（UA）<br>Power consumption（UA）"},
    {"M.27", "成品晃动测试<br>Vibration Testing"}
};
        public readonly Dictionary<string, string> _criteriaMap_JCC_Li_Batt = new Dictionary<string, string>
{
    {"M.1", "参照产品规格书功能指示<br>Refer to product specification for functional instructions"},
    {"M.2", "根据规格书每个功能能正常工作,且LED灯指示正确<br>Each function is tested.The LED display is correct."},
    {"M.3", "包括多发射器配码删码（参照规格书）<br>Test pairing and deleting pairing with multiple remotes(refer to the specification)."},
    {"M.4", "行程设置以及行程调整、行程删除三种（参照规格书）<br>Setting limits,adjusting and deleting limits(refer to the specification)."},
    {"M.5", "设置、删除、运行三种情况（参照规格书）<br>Setting, deleting, running to the favourite position(refer to the specification)"},
    {"M.6", "有无行程分两种操作（参照规格书）<br>With limits and without limits (refer to the specification)"},
    {"M.7", "参照规格书操作<br>Operate according to specification"},
    {"M.8", "参照规格书操作<br>Operate according to specification"},
    {"M.9", "默认6min<br>Default 6min"},
    {"M.10", "断电上电后检查行程点<br>Check the limits after power off and power on"},
    {"M.11", "部分电机有该功能（参考规格书），若无联网功能则填无<br>Only for the motors with this function(refer to the specification).If there is no networking function, fill in 'none'"},
    {"M.12", "USB、Type-C口测试电流， LED灯情况<br>Test the charging current when charging,check the LED display."},
    {"M.13", "根据下发的灵敏度检验规范测试，包括发射器测试数值，以及室内室外拉距离，单控，群控。若为手柄或拉杆电机，此处填无<br>Test the RF signal distance under channel control and group control indoor and outdoor.If it is a handle or pull rod motor, please fill in 'none' here"},
    {"M.14", "请备注具体功能（模块，485干触点，齐平等）<br>Please note specific functions (module, 485, dry contacts,etc."},
    {"M.15", "参照规格书<br>Refer to the specification"},
    {"M.16", "参照规格书<br>Refer to the specification"},
    {"M.17", "填写整机长度<br>Fill in the overall length of the motor."},
    {"M.18", "参照参数表QC-000667<br>Refer to the parameter table QC-000667"},
    {"M.19", "直观明显的外观问题，外观瑕疵<br>Visual obvious appearance defects."},
    {"M.20", "按键手感功能<br>Key feel function"},
    {"M.21", "参考样品需求，若无配件，则填无；若有配件，则填写配件编号<br>Refer to the sample requirements, if there is no accessory, fill in 'none'; Otherwise, fill in the accessory number"},
    {"M.22", "参考样品需求，如内置天线则填写内置天线，如外置天线，需填写天线颜色及外露长度<br>Please refer to the sample requirements. If there is an internal antenna, please fill in the internal antenna. If there is an external antenna, please fill in the antenna color and exposed length"},
    {"M.23", "参考样品需求，若无电源线，则填无；若有电源线，则需填写电源线长度、规格<br>Refer to the sample requirements, if there is no power cable, fill in 'none'; otherwise, fill in the length and specifications of the cable"},
    {"M.24", "电量检测（V）<br>Battery capacity testing（V）<br>电量需在70-80%<br>Battery capacity needs to be 70-80%"},
    {"M.25", "半成品充电<br>Semi-finished charge<br>测试充电电流，截止电压<br>Test charging current, cut-off voltage"},
    {"M.26", "功耗（UA）<br>Power consumption（UA）<br>静态≤120uA<br>Static ≤120uA"},
    {"M.27", "成品晃动测试<br>Vibration Testing<br>制作震动工装或简易震动设备购买<br>Make vibration tooling or simple vibration equipment purchase."}
};
        // 样品部件字典
        public readonly Dictionary<string, string> _sampleComponents_JCC_Li_Batt = new Dictionary<string, string>
{
    {"S.1", "主控PCB板"},
    {"S.2", "霍尔PCB板"},
    {"S.3", "电池包"},
    {"S.4", "马达"},
    {"S.5", "减速组合"},
    {"S.6", "离合器组合"},
    {"S.7", "整机"}
};
        // 要求字典
        public readonly Dictionary<string, string> _requirements_JCC_Li_Batt = new Dictionary<string, string>
{
    {"S.1", "主控PCB板 正反面留底"},
    {"S.2", "霍尔PCB板 正反面留底"},
    {"S.3", "电池包 电池包铭牌、整体留底"},
    {"S.4", "马达 电压、生产日期、整体留底"},
    {"S.5", "减速组合 整体留底"},
    {"S.6", "离合器组合 霍尔磁铁、整体留底"},
    {"S.7", "整机 行程头端、减速端、整机、铭牌、二维码等角度留底"}
};
        //填写内容格式
        public readonly Dictionary<string, string> _fillingtype_JCC_Li_Batt = new Dictionary<string, string>
{
    {"M.1", "1"},
    {"M.2", "1"},
    {"M.3", "1"},
    {"M.4", "1"},
    {"M.5", "1"},
    {"M.6", "1"},
    {"M.7", "1"},
    {"M.8", "0"},
    {"M.9", "1"},
    {"M.10", "1"},
    {"M.11", "1"},
    {"M.12", "1"},
    {"M.13", "0"},
    {"M.14", "0"},
    {"M.15", "1"},
    {"M.16", "1"},
    {"M.17", "1"},
    {"M.18", "1"},
    {"M.19", "1"},
    {"M.20", "1"},
    {"M.21", "0"},
    {"M.22", "0"},
    {"M.23", "0"},
    {"M.24", "1"},
    {"M.25", "1"},
    {"M.26", "1"},
    {"M.27", "1"},
   
};



        /// <summary>
        /// JCC-内置开关电源款开合帘电机
        /// </summary>
        public readonly Dictionary<string, string> _specificationDescMap_JCC_Built_in_Sw_PS = new Dictionary<string, string>
{
    {"M.1", "指示灯<br>LED"},
    {"M.2", "按键功能<br>Motor button function testing"},
    {"M.3", "添码/删码<br>Pairing/Delete pairing"},
    {"M.4", "行程设置<br>Limit setting"},
    {"M.5", "最佳运行位置<br>Favourite position"},
    {"M.6", "换向<br>Direction Reversing"},
    {"M.7", "点动连动切换<br>One-touch/Contant-touch mode switching"},
    {"M.8", "速度切换<br>Speed adjusting"},
    {"M.9", "连续运行时间<br>Continuous running time"},
    {"M.10", "断电测试<br>Power off testing"},
    {"M.11", "联网功能<br>App connecting function"},
    {"M.12", "接收灵敏度<br>RF signal sensitivity"},
    {"M.13", "客户特殊功能要求<br>Customized function testing"},
    {"M.14", "充电提示/唤醒<br>Charging indicate"},
    {"M.15", "成品空载转速<br>Motor speed without loading"},
    {"M.16", "成品长度/管长<br>Motor length/Tube length"},
    {"M.17", "电机空载噪音<br>Motor noise without loading"},
    {"M.18", "电机外观<br>Motor appearance"},
    {"M.19", "按键灵敏度<br>Key sensitivity"},
    {"M.20", "电机配件<br>Motor accessory"},
    {"M.21", "天线<br>Antenna"},
    {"M.22", "电源线长度、规格<br>Power Cable length and specification"},
    {"M.23", "成品晃动测试<br>Vibration Testing"}
};
        public readonly Dictionary<string, string> _criteriaMap_JCC_Built_in_Sw_PS = new Dictionary<string, string>
{
    {"M.1", "参照产品规格书功能指示<br>Refer to product specification for functional instructions"},
    {"M.2", "根据规格书每个功能能正常工作,且LED灯指示正确<br>Each function is tested.The LED display is correct."},
    {"M.3", "包括多发射器配码删码（参照规格书）<br>Test pairing and deleting pairing with multiple remotes(refer to the specification)."},
    {"M.4", "行程设置以及行程调整、行程删除三种（参照规格书）<br>Setting limits,adjusting and deleting limits(refer to the specification)."},
    {"M.5", "设置、删除、运行三种情况（参照规格书）<br>Setting, deleting, running to the favourite position(refer to the specification)"},
    {"M.6", "有无行程分两种操作（参照规格书）<br>With limits and without limits (refer to the specification)"},
    {"M.7", "参照规格书操作<br>Operate according to specification"},
    {"M.8", "参照规格书操作<br>Operate according to specification"},
    {"M.9", "默认6min<br>Default 6min"},
    {"M.10", "断电上电后检查行程点<br>Check the limits after power off and power on"},
    {"M.11", "部分电机有该功能（参考规格书），若无联网功能则填无<br>Only for the motors with this function(refer to the specification).If there is no networking function, fill in 'none'"},
    {"M.12", "根据下发的灵敏度检验规范测试，包括发射器测试数值，以及室内室外拉距离，单控，群控。若为手柄或拉杆电机，此处填无<br>Test the RF signal distance under channel control and group control indoor and outdoor.If it is a handle or pull rod motor, please fill in 'none' here"},
    {"M.13", "请备注具体功能（模块，485干触点，齐平等）<br>Please note specific functions (module, 485, dry contacts,etc."},
    {"M.14", "参照规格书<br>Refer to the specification"},
    {"M.15", "参照规格书<br>Refer to the specification"},
    {"M.16", "填写整机长度<br>Fill in the overall length of the motor."},
    {"M.17", "参照参数表QC-001868<br>Refer to the parameter table QC-001868"},
    {"M.18", "直观明显的外观问题，外观瑕疵<br>Visual obvious appearance defects."},
    {"M.19", "按键手感功能<br>Key feel function"},
    {"M.20", "参考样品需求，若无配件，则填无；若有配件，则填写配件编号<br>Refer to the sample requirements, if there is no accessory, fill in 'none'; Otherwise, fill in the accessory number"},
    {"M.21", "参考样品需求，如内置天线则填写内置天线，如外置天线，需填写天线颜色及外露长度<br>Please refer to the sample requirements. If there is an internal antenna, please fill in the internal antenna. If there is an external antenna, please fill in the antenna color and exposed length"},
    {"M.22", "参考样品需求，若无电源线，则填无；若有电源线，则需填写电源线长度、规格<br>Refer to the sample requirements, if there is no power cable, fill in 'none'; otherwise, fill in the length and specifications of the cable"},
    {"M.23", "成品晃动测试<br>Vibration Testing<br>制作震动工装或简易震动设备购买<br>Make vibration tooling or simple vibration equipment purchase."}
};
        // 样品部件字典
        public readonly Dictionary<string, string> _sampleComponents_JCC_Built_in_Sw_PS = new Dictionary<string, string>
{
    {"S.1", "主控PCB板"},
    {"S.2", "霍尔PCB板"},
    {"S.3", "开关电源板"},
    {"S.4", "电池包"},
    {"S.5", "马达"},
    {"S.6", "减速组合"},
    {"S.7", "离合器组合"},
    {"S.8", "电源线"},
    {"S.9", "整机"}
};
        // 要求字典
        public readonly Dictionary<string, string> _requirements_JCC_Built_in_Sw_PS = new Dictionary<string, string>
{
    {"S.1", "主控PCB板 正反面留底"},
    {"S.2", "霍尔PCB板 正反面留底"},
    {"S.3", "开关电源板 正反面留底"},
    {"S.4", "电池包 正反面留底"},
    {"S.5", "马达 电压、生产日期、整体留底"},
    {"S.6", "减速组合 整体留底"},
    {"S.7", "离合器组合 霍尔磁铁、整体留底"},
    {"S.8", "电源线 供方、芯线数、认证、规格留底"},
    {"S.9", "整机 行程头端、减速端、整机、铭牌、二维码等角度留底"}
};
        //填写内容格式
        public readonly Dictionary<string, string> _fillingtype_JCC_Built_in_Sw_PS = new Dictionary<string, string>
{
    {"M.1", "1"},
    {"M.2", "1"},
    {"M.3", "1"},
    {"M.4", "1"},
    {"M.5", "1"},
    {"M.6", "1"},
    {"M.7", "1"},
    {"M.8", "0"},
    {"M.9", "1"},
    {"M.10", "1"},
    {"M.11", "1"},
    {"M.12", "0"},
    {"M.13", "0"},
    {"M.14", "1"},
    {"M.15", "0"},
    {"M.16", "0"},
    {"M.17", "0"},
    {"M.18", "1"},
    {"M.19", "1"},
    {"M.20", "0"},
    {"M.21", "0"},
    {"M.22", "0"},
    {"M.23", "1"},
    
};



        /// <summary>
        /// JCC-AE款开合帘电机
        /// </summary>
        public readonly Dictionary<string, string> _specificationDescMap_JCC_AE_Type_Curtain = new Dictionary<string, string>
{
    {"M.1", "指示灯<br>LED"},
    {"M.2", "按键功能<br>Motor button function testing"},
    {"M.3", "添码/删码<br>Pairing/Delete pairing"},
    {"M.4", "行程设置<br>Limit setting"},
    {"M.5", "最佳运行位置<br>Favourite position"},
    {"M.6", "换向<br>Direction Reversing"},
    {"M.7", "点动连动切换<br>One-touch/Contant-touch mode switching"},
    {"M.8", "速度切换<br>Speed adjusting"},
    {"M.9", "连续运行时间<br>Continuous running time"},
    {"M.10", "断电测试<br>Power off testing"},
    {"M.11", "联网功能<br>App connecting function"},
    {"M.12", "接收灵敏度<br>RF signal sensitivity"},
    {"M.13", "客户特殊功能要求<br>Customized function testing"},
    {"M.14", "充电提示/唤醒<br>Charging indicate"},
    {"M.15", "成品空载转速<br>Motor speed without loading"},
    {"M.16", "成品长度/管长<br>Motor length/Tube length"},
    {"M.17", "电机空载噪音<br>Motor noise without loading"},
    {"M.18", "电机外观<br>Motor appearance"},
    {"M.19", "按键灵敏度<br>Key sensitivity"},
    {"M.20", "电机配件<br>Motor accessory"},
    {"M.21", "天线<br>Antenna"},
    {"M.22", "电源线长度、规格<br>Power Cable length and specification"},
    {"M.23", "功耗（UA）<br>Power consumption（UA）"},
    {"M.24", "成品晃动测试<br>Vibration Testing"}
};
        public readonly Dictionary<string, string> _criteriaMap_JCC_AE_Type_Curtain = new Dictionary<string, string>
{
    {"M.1", "参照产品规格书功能指示<br>Refer to product specification for functional instructions"},
    {"M.2", "根据规格书每个功能能正常工作,且LED灯指示正确<br>Each function is tested.The LED display is correct."},
    {"M.3", "包括多发射器配码删码（参照规格书）<br>Test pairing and deleting pairing with multiple remotes(refer to the specification)."},
    {"M.4", "行程设置以及行程调整、行程删除三种（参照规格书）<br>Setting limits,adjusting and deleting limits(refer to the specification)."},
    {"M.5", "设置、删除、运行三种情况（参照规格书）<br>Setting, deleting, running to the favourite position(refer to the specification)"},
    {"M.6", "有无行程分两种操作（参照规格书）<br>With limits and without limits (refer to the specification)"},
    {"M.7", "参照规格书操作<br>Operate according to specification"},
    {"M.8", "参照规格书操作<br>Operate according to specification"},
    {"M.9", "默认6min<br>Default 6min"},
    {"M.10", "断电上电后检查行程点<br>Check the limits after power off and power on"},
    {"M.11", "部分电机有该功能（参考规格书），若无联网功能则填无<br>Only for the motors with this function(refer to the specification).If there is no networking function, fill in 'none'"},
    {"M.12", "根据下发的灵敏度检验规范测试，包括发射器测试数值，以及室内室外拉距离，单控，群控。若为手柄或拉杆电机，此处填无<br>Test the RF signal distance under channel control and group control indoor and outdoor.If it is a handle or pull rod motor, please fill in 'none' here"},
    {"M.13", "请备注具体功能（模块，485干触点，齐平等）<br>Please note specific functions (module, 485, dry contacts,etc."},
    {"M.14", "参照规格书<br>Refer to the specification"},
    {"M.15", "参照规格书<br>Refer to the specification"},
    {"M.16", "填写整机长度<br>Fill in the overall length of the motor."},
    {"M.17", "参照参数表QC-000667<br>Refer to the parameter table QC-000667"},
    {"M.18", "直观明显的外观问题，外观瑕疵<br>Visual obvious appearance defects."},
    {"M.19", "按键手感功能<br>Key feel function"},
    {"M.20", "参考样品需求，若无配件，则填无；若有配件，则填写配件编号<br>Refer to the sample requirements, if there is no accessory, fill in 'none'; Otherwise, fill in the accessory number"},
    {"M.21", "参考样品需求，如内置天线则填写内置天线，如外置天线，需填写天线颜色及外露长度<br>Please refer to the sample requirements. If there is an internal antenna, please fill in the internal antenna. If there is an external antenna, please fill in the antenna color and exposed length"},
    {"M.22", "参考样品需求，若无电源线，则填无；若有电源线，则需填写电源线长度、规格<br>Refer to the sample requirements, if there is no power cable, fill in 'none'; otherwise, fill in the length and specifications of the cable"},
    {"M.23", "功耗（UA）（若无低功耗要求，则填无）<br>Power consumption（UA）<br>静态≤120uA<br>Static ≤120uA"},
    {"M.24", "成品晃动测试<br>Vibration Testing<br>制作震动工装或简易震动设备购买<br>Make vibration tooling or simple vibration equipment purchase."}
};
        // 样品部件字典
        public readonly Dictionary<string, string> _sampleComponents_JCC_AE_Type_Curtain = new Dictionary<string, string>
{
    {"S.1", "主控PCB板"},
    {"S.2", "霍尔PCB板"},
    {"S.3", "马达"},
    {"S.4", "减速组合"},
    {"S.5", "离合器组合"},
    {"S.6", "电源线"},
    {"S.7", "整机"}
};
        // 要求字典
        public readonly Dictionary<string, string> _requirements_JCC_AE_Type_Curtain = new Dictionary<string, string>
{
    {"S.1", "主控PCB板 正反面留底"},
    {"S.2", "霍尔PCB板 正反面留底"},
    {"S.3", "马达 电压、生产日期、整体留底"},
    {"S.4", "减速组合 整体留底"},
    {"S.5", "离合器组合 霍尔磁铁、整体留底"},
    {"S.6", "电源线 供方、芯线数、认证、规格留底（无电源线，则附空白图片）"},
    {"S.7", "整机 行程头端、减速端、整机、铭牌、二维码等角度留底"}
};
        //填写内容格式
        public readonly Dictionary<string, string> _fillingtype_JCC_AE_Type_Curtain = new Dictionary<string, string>
{
    {"M.1", "1"},
    {"M.2", "1"},
    {"M.3", "1"},
    {"M.4", "1"},
    {"M.5", "1"},
    {"M.6", "1"},
    {"M.7", "1"},
    {"M.8", "0"},
    {"M.9", "1"},
    {"M.10", "1"},
    {"M.11", "1"},
    {"M.12", "0"},
    {"M.13", "0"},
    {"M.14", "1"},
    {"M.15", "0"},
    {"M.16", "0"},
    {"M.17", "0"},
    {"M.18", "1"},
    {"M.19", "1"},
    {"M.20", "0"},
    {"M.21", "0"},
    {"M.22", "0"},
    {"M.23", "1"},
    {"M.24", "1"},
    
};



        /// <summary>
        /// JCD-LE款直流管状电机
        /// </summary>
        public readonly Dictionary<string, string> _specificationDescMap_JCD_LE_Type_DC = new Dictionary<string, string>
{
    {"M.1", "指示灯<br>LED"},
    {"M.2", "行程头功能<br>Function of the motor head button"},
    {"M.3", "行程头按键功能<br>Motor head button function testing"},
    {"M.4", "添码/删码<br>Pairing/Delete pairing"},
    {"M.5", "行程设置<br>Limit setting"},
    {"M.6", "最佳运行位置<br>Favourite position"},
    {"M.7", "点动测试（1圈）<br>One-touch mode (1 round)"},
    {"M.8", "换向<br>Direction Reversing"},
    {"M.9", "点动连动切换<br>One-touch/Contant-touch mode switching"},
    {"M.10", "速度切换<br>Speed adjusting"},
    {"M.11", "连续运行时间<br>Continuous running time"},
    {"M.12", "断电测试<br>Power off testing"},
    {"M.13", "联网功能<br>App connecting function"},
    {"M.14", "成品充电<br>Charging"},
    {"M.15", "接收灵敏度<br>RF signal sensitivity"},
    {"M.16", "客户特殊功能要求<br>Customized function testing"},
    {"M.17", "充电提示/唤醒<br>Charging indicate"},
    {"M.18", "成品空载转速<br>Motor speed without loading"},
    {"M.19", "成品长度/管长<br>Motor length/Tube length"},
    {"M.20", "电机空载噪音<br>Motor noise without loading"},
    {"M.21", "电机外观<br>Motor appearance"},
    {"M.22", "按键灵敏度<br>Key sensitivity"},
    {"M.23", "电机配件<br>Motor accessory"},
    {"M.24", "天线<br>Antenna"},
    {"M.25", "电源线长度、规格<br>Power Cable length and specification"},
    {"M.26", "遇阻功能检测<br>Obstacle detection function"},
    {"M.27", "电量检测（V）<br>Battery capacity testing（V）"},
    {"M.28", "半成品充电<br>Semi-finished charge"},
    {"M.29", "功耗（UA）<br>Power consumption（UA）"},
    {"M.30", "电机性能（带载）<br>Motor performance with loading"},
    {"M.31", "成品满载转速<br>Motor speed with full loading<br>样册上是空载转速，满载转速跟样册会有差异，此数据仅内部参考"},
    {"M.32", "成品晃动测试<br>Vibration Testing"}
  
};
        public readonly Dictionary<string, string> _criteriaMap_JCD_LE_Type_DC = new Dictionary<string, string>
{
    {"M.1", "参照产品规格书功能指示<br>Refer to product specification for functional instructions"},
    {"M.2", "按键， LED,充电，软排线 以上均正常连接<br>Buttons, LED, charging, soft cable are connected correctly."},
    {"M.3", "根据规格书每个功能能正常工作,且LED灯指示正确<br>Each function is tested.The LED display is correct."},
    {"M.4", "包括多发射器配码删码（参照规格书）<br>Test pairing and deleting pairing with multiple remotes(refer to the specification)."},
    {"M.5", "上下行程以及行程调整、行程删除三种（参照规格书）<br>Setting limits,adjusting and deleting limits(refer to the specification)."},
    {"M.6", "设置、删除、运行三种情况（参照规格书）<br>Setting, deleting, running to the favourite position(refer to the specification)"},
    {"M.7", "符合每次按键电机响应并动作<br>The motor responds and operates by every pressing."},
    {"M.8", "有无行程分两种操作（参照规格书）<br>With limits and without limits (refer to the specification)"},
    {"M.9", "参照规格书操作<br>Operate according to specification"},
    {"M.10", "参照规格书操作<br>Operate according to specification"},
    {"M.11", "默认6min<br>Default 6min"},
    {"M.12", "断电上电后检查行程点<br>Check the limits after power off and power on"},
    {"M.13", "部分电机有该功能（参考规格书），若无联网功能则填无<br>Only for the motors with this function(refer to the specification).If there is no networking function, fill in 'none'"},
    {"M.14", "USB、Type-C口测试电流， LED灯情况<br>Test the charging current when charging,check the LED display."},
    {"M.15", "根据下发的灵敏度检验规范测试，包括发射器测试数值，以及室内室外拉距离，单控，群控。若为手柄或拉杆电机，此处填无<br>Test the RF signal distance under channel control and group control indoor and outdoor.If it is a handle or pull rod motor, please fill in 'none' here"},
    {"M.16", "请备注具体功能（模块，485干触点，齐平等）<br>Please note specific functions (module, 485, dry contacts,etc."},
    {"M.17", "参照规格书<br>Refer to the specification"},
    {"M.18", "参照规格书<br>Refer to the specification"},
    {"M.19", "填写整机长度，若为拉杆或手柄电机，则拉杆或手柄长度需填写<br>Fill in the total length of the motor. If it is a pull rod or handle motor, also fill in the length of the pull rod or handle"},
    {"M.20", "参照参数表QC-000667<br>Refer to the parameter table QC-000667"},
    {"M.21", "直观明显的外观问题，外观瑕疵<br>Visual obvious appearance defects."},
    {"M.22", "按键手感功能<br>Key feel function"},
    {"M.23", "参考样品需求，若无配件，则填无；若有配件，则填写配件编号<br>Refer to the sample requirements, if there is no accessory, fill in 'none'; Otherwise, fill in the accessory number"},
    {"M.24", "参考样品需求，如内置天线则填写内置天线，如外置天线，需填写天线颜色及外露长度<br>Please refer to the sample requirements. If there is an internal antenna, please fill in the internal antenna. If there is an external antenna, please fill in the antenna color and exposed length"},
    {"M.25", "参考样品需求，若无电源线，则填无；若有电源线，则需填写电源线长度、规格<br>Refer to the sample requirements, if there is no power cable, fill in 'none'; otherwise, fill in the length and specifications of the cable"},
    {"M.26", "参照规格书操作<br>Operate according to specification"},
    {"M.27", "电量检测（V）<br>Battery capacity testing（V）<br>电量需在70-80%<br>Battery capacity needs to be 70-80%"},
    {"M.28", "半成品充电<br>Semi-finished charge<br>测试充电电流，截止电压<br>Test charging current, cut-off voltage"},
    {"M.29", "功耗（UA）<br>Power consumption（UA）<br>静态≤120uA<br>Static ≤120uA"},
    {"M.30", "电机性能（带载）<br>Motor performance with loading<br>满负载不打滑，行程正常<br>Slipping does not happen with correct limits"},
    {"M.31", "成品满载转速<br>Motor speed with full loading<br>参照规格书<br>Reference specification<br>样册上是空载转速，满载转速跟样册会有差异，此数据仅内部参考"},
    {"M.32", "成品晃动测试<br>Vibration Testing<br>制作震动工装或简易震动设备购买<br>Make vibration tooling or simple vibration equipment purchase."}
    
};
        // 样品部件字典
        public readonly Dictionary<string, string> _sampleComponents_JCD_LE_Type_DC = new Dictionary<string, string>
{
    {"S.1", "行程头PCB板"},
    {"S.2", "主控PCB板"},
    {"S.3", "霍尔PCB板"},
    {"S.4", "电池包"},
    {"S.5", "减速马达组合"},
    {"S.6", "整机"}
};
        // 要求字典
        public readonly Dictionary<string, string> _requirements_JCD_LE_Type_DC = new Dictionary<string, string>
{
    {"S.1", "行程头PCB板 正反面留底"},
    {"S.2", "主控PCB板 正反面留底"},
    {"S.3", "霍尔PCB板 正反面留底"},
    {"S.4", "电池包 电池包铭牌、整体留底"},
    {"S.5", "减速马达组合 镭雕料号、生产日期、整体留底"},
    {"S.6", "整机 行程头端、减速端、整机、铭牌、二维码等角度留底"}
};
        //填写内容格式
        public readonly Dictionary<string, string> _fillingtype_JCD_LE_Type_DC = new Dictionary<string, string>
{
     {"M.1", "1"},
    {"M.2", "1"},
    {"M.3", "1"},
    {"M.4", "1"},
    {"M.5", "1"},
    {"M.6", "1"},
    {"M.7", "1"},
    {"M.8", "1"},
    {"M.9", "1"},
    {"M.10", "0"},
    {"M.11", "1"},
    {"M.12", "1"},
    {"M.13", "1"},
    {"M.14", "1"},
    {"M.15", "0"},
    {"M.16", "0"},
    {"M.17", "1"},
    {"M.18", "0"},
    {"M.19", "0"},
    {"M.20", "0"},
    {"M.21", "1"},
    {"M.22", "1"},
    {"M.23", "0"},
    {"M.24", "0"},
    {"M.25", "0"},
    {"M.26", "0"},
    {"M.27", "1"},
    {"M.28", "1"},
    {"M.29", "1"},
    {"M.30", "1"},
    {"M.31", "0"},
    {"M.32", "1"},
};


        /// <summary>
        /// JCD-TE款直流管状电机
        /// </summary>
        public readonly Dictionary<string, string> _specificationDescMap_JCD_TE_Type_DC = new Dictionary<string, string>
{
    {"M.1", "指示灯<br>LED"},
    {"M.2", "行程头功能<br>Function of the motor head button"},
    {"M.3", "行程头按键功能<br>Motor head button function testing"},
    {"M.4", "添码/删码<br>Pairing/Delete pairing"},
    {"M.5", "行程设置<br>Limit setting"},
    {"M.6", "最佳运行位置<br>Favourite position"},
    {"M.7", "点动测试（1圈）<br>One-touch mode (1 round)"},
    {"M.8", "换向<br>Direction Reversing"},
    {"M.9", "点动连动切换<br>One-touch/Contant-touch mode switching"},
    {"M.10", "速度切换<br>Speed adjusting"},
    {"M.11", "连续运行时间<br>Continuous running time"},
    {"M.12", "断电测试<br>Power off testing"},
    {"M.13", "联网功能<br>App connecting function"},
    {"M.14", "接收灵敏度<br>RF signal sensitivity"},
    {"M.15", "客户特殊功能要求<br>Customized function testing"},
    {"M.16", "成品空载转速<br>Motor speed without loading"},
    {"M.17", "成品长度/管长<br>Motor length/Tube length"},
    {"M.18", "电机空载噪音<br>Motor noise without loading"},
    {"M.19", "电机外观<br>Motor appearance"},
    {"M.20", "按键灵敏度<br>Key sensitivity"},
    {"M.21", "电机配件<br>Motor accessory"},
    {"M.22", "天线<br>Antenna"},
    {"M.23", "电源线长度、规格<br>Power Cable length and specification"},
    {"M.24", "遇阻功能检测<br>Obstacle detection function"},
    {"M.25", "电机性能（带载）<br>Motor performance with loading"},
    {"M.26", "成品满载转速<br>Motor speed with full loading<br>样册上是空载转速，满载转速跟样册会有差异，此数据仅内部参考"},
    {"M.27", "成品晃动测试<br>Vibration Testing"}
};
        public readonly Dictionary<string, string> _criteriaMap_JCD_TE_Type_DC = new Dictionary<string, string>
{
    {"M.1", "参照产品规格书功能指示<br>Refer to product specification for functional instructions"},
    {"M.2", "按键， LED,软排线 以上均正常连接<br>Buttons, LED, soft cable are connected correctly."},
    {"M.3", "根据规格书每个功能能正常工作,且LED灯指示正确<br>Each function is tested.The LED display is correct."},
    {"M.4", "包括多发射器配码删码（参照规格书）<br>Test pairing and deleting pairing with multiple remotes(refer to the specification)."},
    {"M.5", "上下行程以及行程调整、行程删除三种（参照规格书）<br>Setting limits,adjusting and deleting limits(refer to the specification)."},
    {"M.6", "设置、删除、运行三种情况（参照规格书）<br>Setting, deleting, running to the favourite position(refer to the specification)"},
    {"M.7", "符合每次按键电机响应并动作<br>The motor responds and operates by every pressing."},
    {"M.8", "有无行程分两种操作（参照规格书）<br>With limits and without limits (refer to the specification)"},
    {"M.9", "参照规格书操作<br>Operate according to specification"},
    {"M.10", "参照规格书操作<br>Operate according to specification"},
    {"M.11", "默认6min<br>Default 6min"},
    {"M.12", "断电上电后检查行程点<br>Check the limits after power off and power on"},
    {"M.13", "部分电机有该功能（参考规格书），若无联网功能则填无<br>Only for the motors with this function(refer to the specification).If there is no networking function, fill in 'none'"},
    {"M.14", "根据下发的灵敏度检验规范测试，包括发射器测试数值，以及室内室外拉距离，单控，群控<br>Test the RF signal distance under channel control and group control indoor and outdoor."},
    {"M.15", "请备注具体功能（模块，485干触点，齐平等）<br>Please note specific functions (module, 485, dry contacts,etc."},
    {"M.16", "参照规格书<br>Refer to the specification"},
    {"M.17", "填写整机长度<br>Fill in the overall length of the motor."},
    {"M.18", "参照参数表QC-000667<br>Refer to the parameter table QC-000667"},
    {"M.19", "直观明显的外观问题，外观瑕疵<br>Visual obvious appearance defects."},
    {"M.20", "按键手感功能<br>Key feel function"},
    {"M.21", "参考样品需求，若无配件，则填无；若有配件，则填写配件编号<br>Refer to the sample requirements, if there is no accessory, fill in 'none'; Otherwise, fill in the accessory number"},
    {"M.22", "参考样品需求，如内置天线则填写内置天线，如外置天线，需填写天线颜色及外露长度<br>Please refer to the sample requirements. If there is an internal antenna, please fill in the internal antenna. If there is an external antenna, please fill in the antenna color and exposed length"},
    {"M.23", "参考样品需求，若无电源线，则填无；若有电源线，则需填写电源线长度、规格<br>Refer to the sample requirements, if there is no power cable, fill in 'none'; otherwise, fill in the length and specifications of the cable"},
    {"M.24", "参照规格书操作<br>Operate according to specification"},
    {"M.25", "电机性能（带载）<br>Motor performance with loading<br>满负载不打滑，行程正常<br>Slipping does not happen with correct limits"},
    {"M.26", "成品满载转速<br>Motor speed with full loading<br>参照规格书<br>Reference specification<br>样册上是空载转速，满载转速跟样册会有差异，此数据仅内部参考"},
    {"M.27", "成品晃动测试<br>Vibration Testing<br>制作震动工装或简易震动设备购买<br>Make vibration tooling or simple vibration equipment purchase."}
};
        // 样品部件字典
        public readonly Dictionary<string, string> _sampleComponents_JCD_TE_Type_DC = new Dictionary<string, string>
{
    {"S.1", "行程头PCB板"},
    {"S.2", "主控PCB板"},
    {"S.3", "霍尔PCB板"},
    {"S.4", "开关电源板"},
    {"S.5", "电池包"},
    {"S.6", "减速马达组合"},
    {"S.7", "电源线"},
    {"S.8", "整机"}
};
        // 要求字典
        public readonly Dictionary<string, string> _requirements_JCD_TE_Type_DC = new Dictionary<string, string>
{
    {"S.1", "行程头PCB板 正反面留底"},
    {"S.2", "主控PCB板 正反面留底"},
    {"S.3", "霍尔PCB板 正反面留底"},
    {"S.4", "开关电源板 正反面留底"},
    {"S.5", "电池包 正反面留底"},
    {"S.6", "减速马达组合 镭雕料号、整体留底"},
    {"S.7", "电源线 供方、芯线数、认证、规格留底"},
    {"S.8", "整机 行程头端、减速端、整机、铭牌、二维码等角度留底"}
};
        //填写内容格式
        public readonly Dictionary<string, string> _fillingtype_JCD_TE_Type_DC = new Dictionary<string, string>
{
     {"M.1", "1"},
    {"M.2", "1"},
    {"M.3", "1"},
    {"M.4", "1"},
    {"M.5", "1"},
    {"M.6", "1"},
    {"M.7", "1"},
    {"M.8", "1"},
    {"M.9", "1"},
    {"M.10", "0"},
    {"M.11", "1"},
    {"M.12", "1"},
    {"M.13", "1"},
    {"M.14", "0"},
    {"M.15", "0"},
    {"M.16", "0"},
    {"M.17", "0"},
    {"M.18", "0"},
    {"M.19", "1"},
    {"M.20", "1"},
    {"M.21", "0"},
    {"M.22", "0"},
    {"M.23", "0"},
    {"M.24", "0"},
    {"M.25", "1"},
    {"M.26", "0"},
    {"M.27", "1"},
   
};


        /// <summary>
        ///JCD-AE款直流管状电机
        /// </summary>
        public readonly Dictionary<string, string> _specificationDescMap_JCD_AE_Type_DC = new Dictionary<string, string>
{
    {"M.1", "指示灯<br>LED"},
    {"M.2", "行程头功能<br>Function of the motor head button"},
    {"M.3", "行程头按键功能<br>Motor head button function testing"},
    {"M.4", "添码/删码<br>Pairing/Delete pairing"},
    {"M.5", "行程设置<br>Limit setting"},
    {"M.6", "最佳运行位置<br>Favourite position"},
    {"M.7", "点动测试（1圈）<br>One-touch mode (1 round)"},
    {"M.8", "换向<br>Direction Reversing"},
    {"M.9", "点动连动切换<br>One-touch/Contant-touch mode switching"},
    {"M.10", "速度切换<br>Speed adjusting"},
    {"M.11", "连续运行时间<br>Continuous running time"},
    {"M.12", "断电测试<br>Power off testing"},
    {"M.13", "联网功能<br>App connecting function"},
    {"M.14", "接收灵敏度<br>RF signal sensitivity"},
    {"M.15", "客户特殊功能要求<br>Customized function testing"},
    {"M.16", "成品空载转速<br>Motor speed without loading"},
    {"M.17", "成品长度/管长<br>Motor length/Tube length"},
    {"M.18", "电机空载噪音<br>Motor noise without loading"},
    {"M.19", "电机外观<br>Motor appearance"},
    {"M.20", "按键灵敏度<br>Key sensitivity"},
    {"M.21", "电机配件<br>Motor accessory"},
    {"M.22", "天线<br>Antenna"},
    {"M.23", "电源线长度、规格<br>Power Cable length and specification"},
    {"M.24", "遇阻功能检测<br>Obstacle detection function"},
    {"M.25", "功耗（UA）<br>Power consumption（UA）"},
    {"M.26", "电机性能（带载）<br>Motor performance with loading"},
    {"M.27", "成品满载转速<br>Motor speed with full loading<br>样册上是空载转速，满载转速跟样册会有差异，此数据仅内部参考"},
    {"M.28", "成品晃动测试<br>Vibration Testing"}
};
        public readonly Dictionary<string, string> _criteriaMap_JCD_AE_Type_DC = new Dictionary<string, string>
{
    {"M.1", "参照产品规格书功能指示<br>Refer to product specification for functional instructions"},
    {"M.2", "按键， LED，软排线 以上均正常连接<br>Buttons, LED, soft cable are connected correctly."},
    {"M.3", "根据规格书每个功能能正常工作,且LED灯指示正确<br>Each function is tested.The LED display is correct."},
    {"M.4", "包括多发射器配码删码（参照规格书）<br>Test pairing and deleting pairing with multiple remotes(refer to the specification)."},
    {"M.5", "上下行程以及行程调整、行程删除三种（参照规格书）<br>Setting limits,adjusting and deleting limits(refer to the specification)."},
    {"M.6", "设置、删除、运行三种情况（参照规格书）<br>Setting, deleting, running to the favourite position(refer to the specification)"},
    {"M.7", "符合每次按键电机响应并动作<br>The motor responds and operates by every pressing."},
    {"M.8", "有无行程分两种操作（参照规格书）<br>With limits and without limits (refer to the specification)"},
    {"M.9", "参照规格书操作<br>Operate according to specification"},
    {"M.10", "参照规格书操作<br>Operate according to specification"},
    {"M.11", "默认6min<br>Default 6min"},
    {"M.12", "断电上电后检查行程点<br>Check the limits after power off and power on"},
    {"M.13", "部分电机有该功能（参考规格书）<br>Only for the motors with this function(refer to the specification)."},
    {"M.14", "根据下发的灵敏度检验规范测试，包括发射器测试数值，以及室内室外拉距离，单控，群控<br>Test the RF signal distance under channel control and group control indoor and outdoor."},
    {"M.15", "请备注具体功能（模块，485干触点，齐平等）<br>Please note specific functions (module, 485, dry contacts,etc."},
    {"M.16", "参照规格书<br>Refer to the specification"},
    {"M.17", "填写整机长度<br>Fill in the overall length of the motor."},
    {"M.18", "参照参数表QC-000667<br>Refer to the parameter table QC-000667"},
    {"M.19", "直观明显的外观问题，外观瑕疵<br>Visual obvious appearance defects."},
    {"M.20", "按键手感功能<br>Key feel function"},
    {"M.21", "参考样品需求，若无配件，则填无；若有配件，则填写配件编号<br>Refer to the sample requirements, if there is no accessory, fill in 'none'; Otherwise, fill in the accessory number"},
    {"M.22", "参考样品需求，如内置天线则填写内置天线，如外置天线，需填写天线颜色及外露长度<br>Please refer to the sample requirements. If there is an internal antenna, please fill in the internal antenna. If there is an external antenna, please fill in the antenna color and exposed length"},
    {"M.23", "参考样品需求，若无电源线，则填无；若有电源线，则需填写电源线长度、规格<br>Refer to the sample requirements, if there is no power cable, fill in 'none'; otherwise, fill in the length and specifications of the cable"},
    {"M.24", "参照规格书操作<br>Operate according to specification"},
    {"M.25", "功耗（UA）（若无低功耗要求，则填无）<br>Power consumption（UA）(If low power consumption is required, fill in none)<br>静态≤120uA<br>Static ≤120uA"},
    {"M.26", "电机性能（带载）<br>Motor performance with loading<br>满负载不打滑，行程正常<br>Slipping does not happen with correct limits"},
    {"M.27", "成品满载转速<br>Motor speed with full loading<br>参照规格书<br>Reference specification<br>样册上是空载转速，满载转速跟样册会有差异，此数据仅内部参考"},
    {"M.28", "成品晃动测试<br>Vibration Testing<br>制作震动工装或简易震动设备购买<br>Make vibration tooling or simple vibration equipment purchase."}
};
        // 样品部件字典
        public readonly Dictionary<string, string> _sampleComponents_JCD_AE_Type_DC = new Dictionary<string, string>
{
    {"S.1", "行程头PCB板"},
    {"S.2", "主控PCB板"},
    {"S.3", "霍尔PCB板"},
    {"S.4", "减速马达组合"},
    {"S.5", "整机"}
};
        // 要求字典
        public readonly Dictionary<string, string> _requirements_JCD_AE_Type_DC = new Dictionary<string, string>
{
    {"S.1", "行程头PCB板 正反面留底"},
    {"S.2", "主控PCB板 正反面留底"},
    {"S.3", "霍尔PCB板 正反面留底"},
    {"S.4", "减速马达组合 镭雕料号、整体留底"},
    {"S.5", "整机 行程头端、减速端、整机、铭牌、二维码等角度留底"}
};
        //填写内容格式
        public readonly Dictionary<string, string> _fillingtype_JCD_AE_Type_DC = new Dictionary<string, string>
{
    {"M.1", "1"},
    {"M.2", "1"},
    {"M.3", "1"},
    {"M.4", "1"},
    {"M.5", "1"},
    {"M.6", "1"},
    {"M.7", "1"},
    {"M.8", "1"},
    {"M.9", "1"},
    {"M.10", "0"},
    {"M.11", "1"},
    {"M.12", "1"},
    {"M.13", "1"},
    {"M.14", "0"},
    {"M.15", "0"},
    {"M.16", "0"},
    {"M.17", "0"},
    {"M.18", "0"},
    {"M.19", "1"},
    {"M.20", "1"},
    {"M.21", "0"},
    {"M.22", "0"},
    {"M.23", "0"},
    {"M.24", "0"},
    {"M.25", "1"},
    {"M.26", "1"},
    {"M.27", "0"},
    {"M.28", "1"},
    
};


        /// <summary>
        /// 常规JCV系列管状电机
        /// </summary>
        public readonly Dictionary<string, string> _specificationDescMap_Std_JCV_Series_Tubular = new Dictionary<string, string>
{
    {"M.1", "按键功能<br>Motor button function testing"},
    {"M.2", "添码/删码<br>Pairing/Delete pairing"},
    {"M.3", "行程设置<br>Limit setting"},
    {"M.4", "最佳运行位置<br>Favourite position"},
    {"M.5", "点动测试（1圈）<br>One-touch mode (1 round)"},
    {"M.6", "换向<br>Direction Reversing"},
    {"M.7", "点动连动切换<br>One-touch/Contant-touch mode switching"},
    {"M.8", "速度切换<br>Speed adjusting"},
    {"M.9", "连续运行时间<br>Continuous running time"},
    {"M.10", "断电测试<br>Power off testing"},
    {"M.11", "联网功能<br>App connecting function"},
    {"M.12", "接收灵敏度<br>RF signal sensitivity"},
    {"M.13", "客户特殊功能要求<br>Customized function testing"},
    {"M.14", "成品空载转速<br>Motor speed without loading"},
    {"M.15", "成品长度/管长<br>Motor length/Tube length"},
    {"M.16", "电机空载噪音<br>Motor noise without loading"},
    {"M.17", "电机外观<br>Motor appearance"},
    {"M.18", "按键灵敏度<br>Key sensitivity"},
    {"M.19", "电机配件<br>Motor accessory"},
    {"M.20", "天线<br>Antenna"},
    {"M.21", "电源线长度、规格<br>Power Cable length and specification"},
    {"M.24", "功耗（UA）<br>Power consumption（UA）"},
    {"M.25", "电机性能（带载）<br>Motor performance with loading"},
    {"M.26", "成品满载转速<br>Motor speed with full loading<br>样册上是空载转速，满载转速跟样册会有差异，此数据仅内部参考"},
    {"M.27", "成品晃动测试<br>Vibration Testing"}
};
        public readonly Dictionary<string, string> _criteriaMap_Std_JCV_Series_Tubular = new Dictionary<string, string>
{
    {"M.1", "根据规格书每个功能能正常工作<br>Each function is tested."},
    {"M.2", "包括多发射器配码删码（参照规格书）<br>Test pairing and deleting pairing with multiple remotes(refer to the specification)."},
    {"M.3", "上下行程以及行程调整、行程删除三种（参照规格书）<br>Setting limits,adjusting and deleting limits(refer to the specification)."},
    {"M.4", "设置、删除、运行三种情况（参照规格书）<br>Setting, deleting, running to the favourite position(refer to the specification)"},
    {"M.5", "符合每次按键电机响应并动作<br>The motor responds and operates by every pressing."},
    {"M.6", "有无行程分两种操作（参照规格书）<br>With limits and without limits (refer to the specification)"},
    {"M.7", "参照规格书操作<br>Operate according to specification"},
    {"M.8", "参照规格书操作<br>Operate according to specification"},
    {"M.9", "默认6min<br>Default 6min"},
    {"M.10", "断电上电后检查行程点<br>Check the limits after power off and power on"},
    {"M.11", "部分电机有该功能（参考规格书）<br>Only for the motors with this function(refer to the specification)."},
    {"M.12", "根据下发的灵敏度检验规范测试，包括发射器测试数值，以及室内室外拉距离，单控，群控<br>Test the RF signal distance under channel control and group control indoor and outdoor."},
    {"M.13", "请备注具体功能（模块，485干触点，齐平等）<br>Please note specific functions (module, 485, dry contacts,etc."},
    {"M.14", "参照规格书<br>Refer to the specification"},
    {"M.15", "填写整机长度<br>Fill in the overall length of the motor."},
    {"M.16", "参照参数表QC-000667<br>Refer to the parameter table QC-000667"},
    {"M.17", "直观明显的外观问题，外观瑕疵<br>Visual obvious appearance defects."},
    {"M.18", "按键手感功能<br>Key feel function"},
    {"M.19", "参考样品需求，若无配件，则填无；若有配件，则填写配件编号<br>Refer to the sample requirements, if there is no accessory, fill in 'none'; Otherwise, fill in the accessory number"},
    {"M.20", "参考样品需求，填写天线颜色及外露长度<br>Refer to the sample requirements and fill in the antenna color and exposed length"},
    {"M.21", "参考样品需求，填写电源线长度、规格<br>Refer to the sample requirements and fill in the length and specification of the power cord"},
    {"M.24", "功耗（UA）（若无低功耗要求，则填无）<br>Power consumption（UA）(If low power consumption is required, fill in none)<br>静态≤120uA<br>Static ≤120uA"},
    {"M.25", "电机性能（带载）<br>Motor performance with loading<br>满负载不打滑，行程正常<br>Slipping does not happen with correct limits"},
    {"M.26", "成品满载转速<br>Motor speed with full loading<br>参照规格书<br>Reference specification<br>样册上是空载转速，满载转速跟样册会有差异，此数据仅内部参考"},
    {"M.27", "成品晃动测试<br>Vibration Testing<br>制作震动工装或简易震动设备购买<br>Make vibration tooling or simple vibration equipment purchase."}
};
        // 样品部件字典
        public readonly Dictionary<string, string> _sampleComponents_Std_JCV_Series_Tubular = new Dictionary<string, string>
{
    {"S.1", "主控PCB板"},
    {"S.2", "霍尔PCB板"},
    {"S.3", "减速马达组合"},
    {"S.4", "整机"}
};
        // 要求字典
        public readonly Dictionary<string, string> _requirements_Std_JCV_Series_Tubular = new Dictionary<string, string>
{
    {"S.1", "主控PCB板 正反面留底"},
    {"S.2", "霍尔PCB板 正反面留底"},
    {"S.3", "减速马达组合 镭雕料号、整体留底"},
    {"S.4", "整机 减速端、整机、出线处、铭牌等角度留底"}
};
        //填写内容格式
        public readonly Dictionary<string, string> _fillingtype_Std_JCV_Series_Tubular = new Dictionary<string, string>
{
    {"M.1", "1"},
    {"M.2", "1"},
    {"M.3", "1"},
    {"M.4", "1"},
    {"M.5", "1"},
    {"M.6", "1"},
    {"M.7", "1"},
    {"M.8", "0"},
    {"M.9", "1"},
    {"M.10", "1"},
    {"M.11", "1"},
    {"M.12", "0"},
    {"M.13", "0"},
    {"M.14", "0"},
    {"M.15", "0"},
    {"M.16", "0"},
    {"M.17", "1"},
    {"M.18", "1"},
    {"M.19", "0"},
    {"M.20", "0"},
    {"M.21", "0"},
    {"M.22", "1"},
    {"M.23", "1"},
    {"M.24", "1"},
    {"M.25", "1"},
    {"M.26", "0"},
    {"M.27", "1"},
   
};

        /// <summary>
        /// 内置锂电池款JCV系列管状电机
        /// </summary>

        public readonly Dictionary<string, string> _specificationDescMap_Built_in_Li_Batt_Type_JCV_Series = new Dictionary<string, string>
{
    {"M.1", "指示灯<br>LED"},
    {"M.2", "按键功能<br>Motor head button function testing"},
    {"M.3", "添码/删码<br>Pairing/Delete pairing"},
    {"M.4", "行程设置<br>Limit setting"},
    {"M.5", "最佳运行位置<br>Favourite position"},
    {"M.6", "点动测试（1圈）<br>One-touch mode (1 round)"},
    {"M.7", "换向<br>Direction Reversing"},
    {"M.8", "点动连动切换<br>One-touch/Contant-touch mode switching"},
    {"M.9", "速度切换<br>Speed adjusting"},
    {"M.10", "连续运行时间<br>Continuous running time"},
    {"M.11", "断电测试<br>Power off testing"},
    {"M.12", "联网功能<br>App connecting function"},
    {"M.13", "成品充电<br>Charging"},
    {"M.14", "接收灵敏度<br>RF signal sensitivity"},
    {"M.15", "客户特殊功能要求<br>Customized function testing"},
    {"M.16", "充电提示/唤醒<br>Charging indicate"},
    {"M.17", "成品空载转速<br>Motor speed without loading"},
    {"M.18", "成品长度/管长<br>Motor length/Tube length"},
    {"M.19", "电机空载噪音<br>Motor noise without loading"},
    {"M.20", "电机外观<br>Motor appearance"},
    {"M.21", "按键灵敏度<br>Key sensitivity"},
    {"M.22", "电机配件<br>Motor accessory"},
    {"M.23", "天线<br>Antenna"},
    {"M.24", "电源线长度、规格<br>Power Cable length and specification"},
    {"M.26", "电量检测（V）<br>Battery capacity testing（V）"},
    {"M.27", "半成品充电<br>Semi-finished charge"},
    {"M.28", "功耗（UA）<br>Power consumption（UA）"},
    {"M.29", "电机性能（带载）<br>Motor performance with loading"},
    {"M.30", "成品满载转速<br>Motor speed with full loading<br>样册上是空载转速，满载转速跟样册会有差异，此数据仅内部参考"},
    {"M.31", "成品晃动测试<br>Vibration Testing"}
};
        public readonly Dictionary<string, string> _criteriaMap_Built_in_Li_Batt_Type_JCV_Series = new Dictionary<string, string>
{
    {"M.1", "参照产品规格书功能指示<br>Refer to product specification for functional instructions"},
    {"M.2", "根据规格书每个功能能正常工作,且LED灯指示正确<br>Each function is tested.The LED display is correct."},
    {"M.3", "包括多发射器配码删码（参照规格书）<br>Test pairing and deleting pairing with multiple remotes(refer to the specification)."},
    {"M.4", "上下行程以及行程调整、行程删除三种（参照规格书）<br>Setting limits,adjusting and deleting limits(refer to the specification)."},
    {"M.5", "设置、删除、运行三种情况（参照规格书）<br>Setting, deleting, running to the favourite position(refer to the specification)"},
    {"M.6", "符合每次按键电机响应并动作<br>The motor responds and operates by every pressing."},
    {"M.7", "有无行程分两种操作（参照规格书）<br>With limits and without limits (refer to the specification)"},
    {"M.8", "参照规格书操作<br>Operate according to specification"},
    {"M.9", "参照规格书操作<br>Operate according to specification"},
    {"M.10", "默认6min<br>Default 6min"},
    {"M.11", "断电上电后检查行程点<br>Check the limits after power off and power on"},
    {"M.12", "部分电机有该功能（参考规格书），若无联网功能则填无<br>Only for the motors with this function(refer to the specification).If there is no networking function, fill in 'none'"},
    {"M.13", "USB、Type-C口、JST测试电流， LED灯情况<br>Test the charging current when charging,check the LED display."},
    {"M.14", "根据下发的灵敏度检验规范测试，包括发射器测试数值，以及室内室外拉距离，单控，群控。若为手柄或拉杆电机，此处填无<br>Test the RF signal distance under channel control and group control indoor and outdoor.If it is a handle or pull rod motor, please fill in 'none' here"},
    {"M.15", "请备注具体功能（模块，485干触点，齐平等）<br>Please note specific functions (module, 485, dry contacts,etc."},
    {"M.16", "参照规格书<br>Refer to the specification"},
    {"M.17", "参照规格书<br>Refer to the specification"},
    {"M.18", "填写整机长度，若为拉杆或手柄电机，则拉杆或手柄长度需填写<br>Fill in the total length of the motor. If it is a pull rod or handle motor, full in the length of the pull rod or handle"},
    {"M.19", "参照参数表QC-000667<br>Refer to the parameter table QC-000667"},
    {"M.20", "直观明显的外观问题，外观瑕疵<br>Visual obvious appearance defects."},
    {"M.21", "按键手感功能<br>Key feel function"},
    {"M.22", "参考样品需求，若无配件，则填无；若有配件，则填写配件编号<br>Refer to the sample requirements, if there is no accessory, fill in 'none'; Otherwise, fill in the accessory number"},
    {"M.23", "参考样品需求，如内置天线则填写内置天线，如外置天线，需填写天线颜色及外露长度<br>Please refer to the sample requirements. If there is an internal antenna, please fill in the internal antenna. If there is an external antenna, please fill in the antenna color and exposed length"},
    {"M.24", "参考样品需求，若无电源线，则填无；若有电源线，则需填写电源线长度、规格<br>Refer to the sample requirements, if there is no power cable, fill in 'none'; otherwise, fill in the length and specifications of the cable"},
    {"M.26", "电量检测（V）<br>Battery capacity testing（V）<br>电量需在70-80%<br>Battery capacity needs to be 70-80%"},
    {"M.27", "半成品充电<br>Semi-finished charge<br>测试充电电流，截止电压<br>Test charging current, cut-off voltage"},
    {"M.28", "功耗（UA）<br>Power consumption（UA）<br>静态≤120uA<br>Static ≤120uA"},
    {"M.29", "电机性能（带载）<br>Motor performance with loading<br>满负载不打滑，行程正常<br>Slipping does not happen with correct limits"},
    {"M.30", "成品满载转速<br>Motor speed with full loading<br>参照规格书<br>Reference specification<br>样册上是空载转速，满载转速跟样册会有差异，此数据仅内部参考"},
    {"M.31", "成品晃动测试<br>Vibration Testing<br>制作震动工装或简易震动设备购买<br>Make vibration tooling or simple vibration equipment purchase."}
};
        // 样品部件字典
        public readonly Dictionary<string, string> _sampleComponents_Built_in_Li_Batt_Type_JCV_Series = new Dictionary<string, string>
{
    {"S.1", "主控PCB板、行程头板"},
    {"S.2", "霍尔PCB板"},
    {"S.3", "电池包"},
    {"S.4", "减速马达组合"},
    {"S.5", "整机"}
};
        // 要求字典
        public readonly Dictionary<string, string> _requirements_Built_in_Li_Batt_Type_JCV_Series = new Dictionary<string, string>
{
    {"S.1", "主控PCB板、行程头板 正反面留底"},
    {"S.2", "霍尔PCB板 正反面留底"},
    {"S.3", "电池包 电池包铭牌、整体留底"},
    {"S.4", "减速马达组合 镭雕料号、生产日期、整体留底"},
    {"S.5", "整机 行程头端、减速端、整机、铭牌、二维码等角度留底"}
};
        //填写内容格式
        public readonly Dictionary<string, string> _fillingtype_Built_in_Li_Batt_Type_JCV_Series = new Dictionary<string, string>
{
    {"M.1", "1"},
    {"M.2", "1"},
    {"M.3", "1"},
    {"M.4", "1"},
    {"M.5", "1"},
    {"M.6", "1"},
    {"M.7", "1"},
    {"M.8", "1"},
    {"M.9", "0"},
    {"M.10", "1"},
    {"M.11", "1"},
    {"M.12", "1"},
    {"M.13", "1"},
    {"M.14", "0"},
    {"M.15", "0"},
    {"M.16", "1"},
    {"M.17", "0"},
    {"M.18", "0"},
    {"M.19", "0"},
    {"M.20", "1"},
    {"M.21", "1"},
    {"M.22", "0"},
    {"M.23", "0"},
    {"M.24", "0"},
    {"M.25", "1"},
    {"M.26", "1"},
    {"M.27", "1"},
    {"M.28", "1"},
    {"M.29", "1"},
    {"M.30", "0"},
    {"M.31", "1"},
};


        /// <summary>
        /// 内置开关电源款JCV系列管状电机
        /// </summary>
        public readonly Dictionary<string, string> _specificationDescMap_Built_in_Sw_PS_JCV_Series_Tubular = new Dictionary<string, string>
{
    {"M.1", "指示灯<br>LED"},
    {"M.2", "行程头功能<br>Function of the motor head button"},
    {"M.3", "行程头按键功能<br>Motor head button function testing"},
    {"M.4", "添码/删码<br>Pairing/Delete pairing"},
    {"M.5", "行程设置<br>Limit setting"},
    {"M.6", "最佳运行位置<br>Favourite position"},
    {"M.7", "点动测试（1圈）<br>One-touch mode (1 round)"},
    {"M.8", "换向<br>Direction Reversing"},
    {"M.9", "点动连动切换<br>One-touch/Contant-touch mode switching"},
    {"M.10", "速度切换<br>Speed adjusting"},
    {"M.11", "连续运行时间<br>Continuous running time"},
    {"M.12", "断电测试<br>Power off testing"},
    {"M.13", "联网功能<br>App connecting function"},
    {"M.14", "接收灵敏度<br>RF signal sensitivity"},
    {"M.15", "客户特殊功能要求<br>Customized function testing"},
    {"M.16", "成品空载转速<br>Motor speed without loading"},
    {"M.17", "成品长度/管长<br>Motor length/Tube length"},
    {"M.18", "电机空载噪音<br>Motor noise without loading"},
    {"M.19", "电机外观<br>Motor appearance"},
    {"M.20", "按键灵敏度<br>Key sensitivity"},
    {"M.21", "电机配件<br>Motor accessory"},
    {"M.22", "天线<br>Antenna"},
    {"M.23", "电源线长度、规格<br>Power Cable length and specification"},
    {"M.24", "电机性能（带载）<br>Motor performance with loading"},
    {"M.25", "成品满载转速<br>Motor speed with full loading<br>样册上是空载转速，满载转速跟样册会有差异，此数据仅内部参考"},
    {"M.26", "成品晃动测试<br>Vibration Testing"}
};
        public readonly Dictionary<string, string> _criteriaMap_Built_in_Sw_PS_JCV_Series_Tubular = new Dictionary<string, string>
{
    {"M.1", "参照产品规格书功能指示<br>Refer to product specification for functional instructions"},
    {"M.2", "按键， LED,充电，软排线 以上均正常连接<br>Buttons, LED, charging, soft cable are connected correctly."},
    {"M.3", "根据规格书每个功能能正常工作,且LED灯指示正确<br>Each function is tested.The LED display is correct."},
    {"M.4", "包括多发射器配码删码（参照规格书）<br>Test pairing and deleting pairing with multiple remotes(refer to the specification)."},
    {"M.5", "上下行程以及行程调整、行程删除三种（参照规格书）<br>Setting limits,adjusting and deleting limits(refer to the specification)."},
    {"M.6", "设置、删除、运行三种情况（参照规格书）<br>Setting, deleting, running to the favourite position(refer to the specification)"},
    {"M.7", "符合每次按键电机响应并动作<br>The motor responds and operates by every pressing."},
    {"M.8", "有无行程分两种操作（参照规格书）<br>With limits and without limits (refer to the specification)"},
    {"M.9", "参照规格书操作<br>Operate according to specification"},
    {"M.10", "参照规格书操作<br>Operate according to specification"},
    {"M.11", "默认6min<br>Default 6min"},
    {"M.12", "断电上电后检查行程点<br>Check the limits after power off and power on"},
    {"M.13", "部分电机有该功能（参考规格书），若无联网功能则填无<br>Only for the motors with this function(refer to the specification).If there is no networking function, fill in 'none'"},
    {"M.14", "根据下发的灵敏度检验规范测试，包括发射器测试数值，以及室内室外拉距离，单控，群控<br>Test the RF signal distance under channel control and group control indoor and outdoor."},
    {"M.15", "请备注具体功能（模块，485干触点，齐平等）<br>Please note specific functions (module, 485, dry contacts,etc."},
    {"M.16", "参照规格书<br>Refer to the specification"},
    {"M.17", "填写整机长度<br>Fill in the overall length of the motor."},
    {"M.18", "参照参数表QC-000667<br>Refer to the parameter table QC-000667"},
    {"M.19", "直观明显的外观问题，外观瑕疵<br>Visual obvious appearance defects."},
    {"M.20", "按键手感功能<br>Key feel function"},
    {"M.21", "参考样品需求，若无配件，则填无；若有配件，则填写配件编号<br>Refer to the sample requirements, if there is no accessory, fill in 'none'; Otherwise, fill in the accessory number"},
    {"M.22", "参考样品需求，如内置天线则填写内置天线，如外置天线，需填写天线颜色及外露长度<br>Please refer to the sample requirements. If there is an internal antenna, please fill in the internal antenna. If there is an external antenna, please fill in the antenna color and exposed length"},
    {"M.23", "参考样品需求，若无电源线，则填无；若有电源线，则需填写电源线长度、规格<br>Refer to the sample requirements, if there is no power cable, fill in 'none'; otherwise, fill in the length and specifications of the cable"},
    {"M.24", "电机性能（带载）<br>Motor performance with loading<br>满负载不打滑，行程正常<br>Slipping does not happen with correct limits"},
    {"M.25", "成品满载转速<br>Motor speed with full loading<br>参照规格书<br>Reference specification<br>样册上是空载转速，满载转速跟样册会有差异，此数据仅内部参考"},
    {"M.26", "成品晃动测试<br>Vibration Testing<br>制作震动工装或简易震动设备购买<br>Make vibration tooling or simple vibration equipment purchase."}
};
        // 样品部件字典
        public readonly Dictionary<string, string> _sampleComponents_Built_in_Sw_PS_JCV_Series_Tubular = new Dictionary<string, string>
{
    {"S.1", "主控PCB板、行程头板"},
    {"S.2", "霍尔PCB板"},
    {"S.3", "开关电源板"},
    {"S.4", "电池包"},
    {"S.5", "减速马达组合"},
    {"S.6", "电源线"},
    {"S.7", "整机"}
};
        // 要求字典
        public readonly Dictionary<string, string> _requirements_Built_in_Sw_PS_JCV_Series_Tubular = new Dictionary<string, string>
{
    {"S.1", "主控PCB板、行程头板 正反面留底"},
    {"S.2", "霍尔PCB板 正反面留底"},
    {"S.3", "开关电源板 正反面留底"},
    {"S.4", "电池包 正反面留底"},
    {"S.5", "减速马达组合 镭雕料号、整体留底"},
    {"S.6", "电源线 供方、芯线数、认证、规格留底"},
    {"S.7", "整机 行程头端、减速端、整机、铭牌、二维码等角度留底"}
};
        //填写内容格式
        public readonly Dictionary<string, string> _fillingtype_Built_in_Sw_PS_JCV_Series_Tubular = new Dictionary<string, string>
            {
                {"M.1", "1"},
                {"M.2", "1"},
                {"M.3", "1"},
                {"M.4", "1"},
                {"M.5", "1"},
                {"M.6", "1"},
                {"M.7", "1"},
                {"M.8", "1"},
                {"M.9", "1"},
                {"M.10", "0"},
                {"M.11", "1"},
                {"M.12", "1"},
                {"M.13", "1"},
                {"M.14", "0"},
                {"M.15", "0"},
                {"M.16", "0"},
                {"M.17", "0"},
                {"M.18", "0"},
                {"M.19", "1"},
                {"M.20", "1"},
                {"M.21", "0"},
                {"M.22", "0"},
                {"M.23", "0"},
                {"M.24", "1"},
                {"M.25", "0"},
                {"M.26", "1"}
    
            };


        /// <summary>
        /// 一次电池款双向遥控器
        /// </summary>
        public readonly Dictionary<string, string> _specificationDescMap_Disposable_Bidirectional_Remote = new Dictionary<string, string>
    {
        {"M.1", "线路板外观<br>Circuit board appearance"},
        {"M.2", "整机外观<br>Overall appearance of the product"},
        {"M.3", "LOGO<br>LOGO"},
        {"M.4", "标签铭牌<br>Label"},
        {"M.5", "重量<br>Weight"},
        {"M.6", "螺钉<br>Screw"},
        {"M.7", "发射电流（mA）<br>Emission Current (mA)"},
        {"M.8", "待机电流（UA）<br>Standby Current (UA)"},
        {"M.9", "进入休眠时间<br>Enter sleep mode"},
        {"M.10", "低电量提示<br>Low battery warning"},
        {"M.11", "发射强度<br>Emission intensity"},
        {"M.12", "中心频率偏移<br>Carrier deviation"},
        {"M.13", "LED显示<br>LED display"},
        {"M.14", "LCD显示<br>LCD display"},
        {"M.15", "实体按键<br>Physical button"},
        {"M.16", "触摸按键<br>Touch button"},
        {"M.17", "滚轮<br>Scroll wheel"},
        {"M.18", "通道切换与通道设置<br>Channel Switching and Channel Settings"},
        {"M.19", "群组切换与群组设置<br>Group Switching and Group Settings"},
        {"M.20", "群组内通道设置<br>Channel Settings in Group"},
        {"M.21", "定时设置<br>Timer Settings"},
        {"M.22", "充电功能<br>Charging function"},
        {"M.23", "单双向切换<br>OOK/FSK Switch"},
        {"M.24", "对码/删码/滚码<br>Code matching / Code deletion / Rolling code"},
        {"M.25", "群控控制<br>Group Control"},
        {"M.26", "换向功能<br>Reversing function"},
        {"M.27", "行程设置<br>Limits Settings"},
        {"M.28", "最佳行程设置<br>Favorite limit Settings"},
        {"M.29", "速度切换<br>Speed switch"},
        {"M.30", "百分比运行<br>Percentage Run"},
        {"M.31", "童锁<br>Lock"},
        {"M.32", "点动与续动切换<br>Jog and continuous mode switch"},
        {"M.33", "检测电机电池电量<br>Check motor battery level"},
        {"M.34", "检测电机位置<br>Detect motor position"},
        {"M.35", "掉电保存<br>Power-off preservation"},
        {"M.36", "定时发射功能<br>Scheduled launch function"},
        {"M.37", "客户特殊功能要求<br>Customer special feature requirements"},
        {"M.38", "远距离遥控测试<br>Long-distance remote control test"},
        {"M.39", "远距离接收测试<br>Long-distance reception test"},
        {"M.40", "恢复出厂设置<br>Factory reset"},
        {"M.41", "电池盖拆卸<br>Battery cover disassembly"},
        {"M.42", "成品晃动测试<br>Finished Product Shake Test"},
        {"M.43", "配件<br>Accessories"}
    };

        public readonly Dictionary<string, string> _criteriaMap_Disposable_Bidirectional_Remote = new Dictionary<string, string>
    {
        {"M.1", "电路板焊接整齐、美观，元器件位置正确，线路板上无明显珠，焊渣<br>The circuit board is neatly and beautifully soldered, with correctly arranged components and no visible solder balls or residues on the board."},
        {"M.2", "整体外壳外观完好，色泽一致，无明显划伤、碰伤、敲击痕<br>The overall exterior casing is in good condition, with a consistent color and no obvious scratches, dents, or impact marks."},
        {"M.3", "LOGO图标清晰，准确，无残缺<br>The logo icon is clear, accurate, and intact."},
        {"M.4", "印刷清楚、正确，无明显倾斜、翘起现象<br>Clearly and correctly printed, without obvious tilting or lifting."},
        {"M.5", "参照说明书<br>Refer to the manual"},
        {"M.6", "不少打螺钉,每个螺钉都拧到位<br>Tighten quite a few screws, making sure each one is securely fastened."},
        {"M.7", "参照规格书<br>Refer to the specifications"},
        {"M.8", "≤15uA"},
        {"M.9", "≤10S"},
        {"M.10", "参照规格书供电出现低电量提示<br>Low battery warning appears when powered according to the specification"},
        {"M.11", "10CM≥-30DB"},
        {"M.12", "±100KHz"},
        {"M.13", "亮度均匀，提示内容都正确<br>The brightness is even, and all the prompts are correct."},
        {"M.14", "显示清晰，提示内容都正确<br>The display is clear, and the prompts are all correct."},
        {"M.15", "所有按键功能正常，高度一致，手感一致，回弹有力，无卡死<br>All keys function normally, have consistent height and feel, with strong rebound and no sticking."},
        {"M.16", "触摸交互提示正常，相邻按键互不影响<br>Touch interaction prompts are normal, and adjacent keys do not affect each other."},
        {"M.17", "滚轮滑动或按下无卡死现象<br>No freezing when scrolling or pressing the wheel"},
        {"M.18", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.19", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.20", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.21", "设置时间，星期正常<br>Set the time, the weekday is normal"},
        {"M.22", "充电电流大于等于0.5C，充电提示显示正常<br>Charging current is greater than or equal to 0.5C, and the charging indicator displays normally."},
        {"M.23", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.24", "正常添码删码，有滚码<br>Normal code addition and deletion, there is a rolling code"},
        {"M.25", "每个通道电机都能被群控控制<br>Each channel motor can be controlled by group control."},
        {"M.26", "有无行程分两种操作（参照规格书）<br>Two operations with/without limits (refer to the specification)"},
        {"M.27", "上下行程以及行程调整、行程删除操作正常（参照规格书）<br>Setting upper limits and lower limits, as well as limits adjustment and limits deletion operations, are normal (refer to the specification)."},
        {"M.28", "设置、删除、运行操作正常（参照规格书）<br>Settings, deletion, and operation run normally (refer to the specification)"},
        {"M.29", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.30", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.31", "参照规格书能够及时刷新图标，组合键功能操作取消<br>Icons can be refreshed in a timely manner according to the specifications, and the function of the shortcut keys has been disabled."},
        {"M.32", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.33", "参照规格书提示电机的电量<br>Check the motor's power according to the specification manual"},
        {"M.34", "参照规格书提示电机的位置<br>Refer to the specifications to locate the motor"},
        {"M.35", "设置好的通道数，群组数，定时数据等相应数据在遥控器彻底断电皆能保存<br>The set number of channels, number of groups, scheduled data, and other corresponding settings can all be retained even when the remote control is completely powered off."},
        {"M.36", "参照规格书能够正常按照设定时间运行<br>It can operate normally according to the set time as specified in the specification."},
        {"M.37", "请备注具体功能，若无填NA<br>Please specify the exact function; if none, fill in NA."},
        {"M.38", "室内≥40m<br>Indoor ≥40m"},
        {"M.39", "室内≥20m<br>Indoor ≥20m"},
        {"M.40", "参照规格书操作恢复出厂<br>Factory reset per specification"},
        {"M.41", "反复拆装20次，要求电池盖/仓无晃动。"},
        {"M.42", "手持样品进行摇晃5次，要求样品不能出现异响或者内部零件有窜动现象<br>Hold the sample and shake it 5 times, ensuring that no abnormal noises occur and that none of the internal components move."},
        {"M.43", "配件齐全，外观良好<br>Complete accessories, good appearance"}
    };
  
        public readonly Dictionary<string, string> _fillingtype_Disposable_Bidirectional_Remote = new Dictionary<string, string>
{
   {"M.1", "1"},
    {"M.2", "1"},
    {"M.3", "1"},
    {"M.4", "1"},
    {"M.5", "0"},
    {"M.6", "1"},
    {"M.7", "0"},
    {"M.8", "0"},
    {"M.9", "1"},
    {"M.10", "1"},
    {"M.11", "0"},
    {"M.12", "1"},
    {"M.13", "1"},
    {"M.14", "1"},
    {"M.15", "1"},
    {"M.16", "1"},
    {"M.17", "1"},
    {"M.18", "1"},
    {"M.19", "1"},
    {"M.20", "1"},
    {"M.21", "1"},
    {"M.22", "0"},
    {"M.23", "1"},
    {"M.24", "1"},
    {"M.25", "1"},
    {"M.26", "1"},
    {"M.27", "1"},
    {"M.28", "1"},
    {"M.29", "1"},
    {"M.30", "1"},
    {"M.31", "1"},
    {"M.32", "1"},
    {"M.33", "1"},
    {"M.34", "0"},
    {"M.35", "0"},
    {"M.36", "1"},
    {"M.37", "1"},
    {"M.38", "1"},
    {"M.39", "1"},
    {"M.40", "1"},
    {"M.41", "1"},
    {"M.42", "1"},
    {"M.43", "1"}


};

        /// <summary>
        /// 一次电池款单向遥控器
        /// </summary>
        public readonly Dictionary<string, string> _specificationDescMap_Disposable_Unidirectional_Remote = new Dictionary<string, string>
    {
        {"M.1", "线路板外观<br>Circuit board appearance"},
        {"M.2", "整机外观<br>Overall appearance of the product"},
        {"M.3", "LOGO<br>LOGO"},
        {"M.4", "标签铭牌<br>Label"},
        {"M.5", "重量<br>Weight"},
        {"M.6", "螺钉<br>Screw"},
        {"M.7", "发射电流（mA）<br>Emission Current (mA)"},
        {"M.8", "待机电流（UA）<br>Standby Current (UA)"},
        {"M.9", "进入休眠时间<br>Enter sleep mode"},
        {"M.10", "低电量提示<br>Low battery warning"},
        {"M.11", "发射强度<br>Emission intensity"},
        {"M.12", "中心频率偏移<br>Carrier deviation"},
        {"M.13", "LED显示<br>LED display"},
        {"M.14", "LCD显示<br>LCD display"},
        {"M.15", "实体按键<br>Physical button"},
        {"M.16", "触摸按键<br>Touch button"},
        {"M.17", "滚轮<br>Scroll wheel"},
        {"M.18", "通道切换与通道设置<br>Channel Switching and Channel Settings"},
        {"M.19", "群组切换与群组设置<br>Group Switching and Group Settings"},
        {"M.20", "群组内通道设置<br>Channel Settings in Group"},
        {"M.21", "定时设置<br>Timer Settings"},
        {"M.22", "充电功能<br>Charging function"},
        {"M.23", "对码/删码/滚码<br>Code matching / Code deletion / Rolling code"},
        {"M.24", "群控控制<br>Group Control"},
        {"M.25", "换向功能<br>Reversing function"},
        {"M.26", "行程设置<br>Limits Settings"},
        {"M.27", "最佳行程设置<br>Favorite limit Settings"},
        {"M.28", "速度切换<br>Speed switch"},
        {"M.29", "百分比运行<br>Percentage Run"},
        {"M.30", "童锁<br>Lock"},
        {"M.31", "点动与续动切换<br>Jog and continuous mode switch"},
        {"M.32", "掉电保存<br>Power-off preservation"},
        {"M.33", "定时发射功能<br>Scheduled launch function"},
        {"M.34", "客户特殊功能要求<br>Customer special feature requirements"},
        {"M.35", "远距离遥控测试<br>Long-distance remote control test"},
        {"M.36", "恢复出厂设置<br>Factory reset"},
        {"M.37", "电池盖拆卸<br>Battery cover removal"},
        {"M.38", "成品晃动测试<br>Finished Product Shake Test"},
        {"M.39", "配件<br>Accessories"}
    };

        public readonly Dictionary<string, string> _criteriaMap_Disposable_Unidirectional_Remote = new Dictionary<string, string>
    {
        {"M.1", "电路板焊接整齐、美观，元器件位置正确，线路板上无明显锡珠、焊渣<br>The circuit board is neatly and beautifully soldered, with correctly arranged components and no visible solder balls or residues on the board."},
        {"M.2", "整体外壳外观完好，色泽一致，无明显划伤、碰伤、敲击痕<br>The overall exterior casing is in good condition, with a consistent color and no obvious scratches, dents, or impact marks."},
        {"M.3", "LOGO图标清晰、准确，无残缺<br>The logo icon is clear, accurate, and intact."},
        {"M.4", "印刷清楚、正确，无明显倾斜、翘起现象<br>Clearly and correctly printed, without obvious tilting or lifting."},
        {"M.5", "参照说明书<br>Refer to the manual"},
        {"M.6", "不少打螺钉，每个螺钉都拧到位<br>Tighten all screws, making sure each one is securely fastened."},
        {"M.7", "参照规格书<br>Refer to the specifications"},
        {"M.8", "≤15uA"},
        {"M.9", "≤10S"},
        {"M.10", "参照规格书供电出现低电量提示<br>Low battery warning appears when powered according to the specification"},
        {"M.11", "10CM≥-30DB"},
        {"M.12", "±100KHz"},
        {"M.13", "亮度均匀，提示内容都正确<br>The brightness is even, and all the prompts are correct."},
        {"M.14", "显示清晰，提示内容都正确<br>The display is clear, and the prompts are all correct."},
        {"M.15", "所有按键功能正常，高度一致，手感一致，回弹有力，无卡死<br>All keys function normally, have consistent height and feel, with strong rebound and no sticking."},
        {"M.16", "触摸交互提示正常，相邻按键互不影响<br>Touch interaction prompts are normal, and adjacent keys do not affect each other."},
        {"M.17", "滚轮滑动或按下无卡死现象<br>No freezing when scrolling or pressing the wheel"},
        {"M.18", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.19", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.20", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.21", "设置时间、星期正常<br>Set the time and weekday normally"},
        {"M.22", "充电电流大于等于0.5C，充电提示显示正常<br>Charging current is greater than or equal to 0.5C, and the charging indicator displays normally."},
        {"M.23", "正常添码删码，有滚码<br>Normal code addition and deletion, with rolling code function"},
        {"M.24", "每个通道电机都能被群控控制<br>Each channel motor can be controlled by group control."},
        {"M.25", "有无行程分两种操作（参照规格书）<br>Two operations with/without limits (refer to the specification)"},
        {"M.26", "上下行程以及行程调整、行程删除操作正常（参照规格书）<br>Setting upper/lower limits, adjustment and deletion operate normally (refer to the specification)."},
        {"M.27", "设置、删除、运行操作正常（参照规格书）<br>Settings, deletion, and operation run normally (refer to the specification)"},
        {"M.28", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.29", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.30", "参照规格书能够及时刷新图标，组合键功能操作取消<br>Icons can be refreshed in a timely manner according to the specifications, and the combination key function is disabled."},
        {"M.31", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.32", "设置好的通道数、群组数、定时数据等相应数据在遥控器彻底断电后皆能保存<br>The set number of channels, groups, scheduled data and other settings can all be retained after the remote control is completely powered off."},
        {"M.33", "参照规格书能够正常按照设定时间运行<br>It can operate normally according to the set time as specified in the specification."},
        {"M.34", "请备注具体功能，若无填NA<br>Please specify the exact function; if none, fill in NA."},
        {"M.35", "室内≥40m<br>Indoor ≥40m"},
        {"M.36", "参照规格书操作恢复出厂<br>Factory reset per specification"},
        {"M.37", "反复拆装20次，要求电池盖/仓无晃动<br>Disassemble and reassemble 20 times, requiring the battery cover/compartment to have no looseness."},
        {"M.38", "手持样品进行摇晃5次，要求样品不能出现异响或者内部零件有窜动现象<br>Hold the sample and shake it 5 times, ensuring that no abnormal noises occur and no internal components move."},
        {"M.39", "配件齐全，外观良好<br>Complete accessories with good appearance"}
    };

        public readonly Dictionary<string, string> _fillingtype_Disposable_Unidirectional_Remote = new Dictionary<string, string>
{
    {"M.1", "1"},
    {"M.2", "1"},
    {"M.3", "1"},
    {"M.4", "1"},
    {"M.5", "0"},
    {"M.6", "1"},
    {"M.7", "0"},
    {"M.8", "0"},
    {"M.9", "1"},
    {"M.10", "1"},
    {"M.11", "0"},
    {"M.12", "1"},
    {"M.13", "1"},
    {"M.14", "1"},
    {"M.15", "1"},
    {"M.16", "1"},
    {"M.17", "1"},
    {"M.18", "1"},
    {"M.19", "1"},
    {"M.20", "1"},
    {"M.21", "1"},
    {"M.22", "0"},
    {"M.23", "1"},
    {"M.24", "1"},
    {"M.25", "1"},
    {"M.26", "1"},
    {"M.27", "1"},
    {"M.28", "1"},
    {"M.29", "1"},
    {"M.30", "1"},
    {"M.31", "1"},
    {"M.32", "1"},
    {"M.33", "1"},
    {"M.34", "0"},
    {"M.35", "0"},
    {"M.36", "1"},
    {"M.37", "1"},
    {"M.38", "1"},
    {"M.39", "1"},
    


};


        /// <summary>
        /// 充电款双向遥控器
        /// </summary>

     

        public readonly Dictionary<string, string> _specificationDescMap_Rechargeable_Bidirectional_Remote = new Dictionary<string, string>
    {
        {"M.1", "线路板外观<br>Circuit board appearance"},
        {"M.2", "整机外观<br>Overall appearance of the product"},
        {"M.3", "LOGO<br>LOGO"},
        {"M.4", "标签铭牌<br>Label"},
        {"M.5", "重量<br>Weight"},
        {"M.6", "螺钉<br>Screw"},
        {"M.7", "发射电流（mA）<br>Emission Current (mA)"},
        {"M.8", "待机电流（UA）<br>Standby Current (UA)"},
        {"M.9", "进入休眠时间<br>Enter sleep mode"},
        {"M.10", "低电量提示<br>Low battery warning"},
        {"M.11", "发射强度<br>Emission intensity"},
        {"M.12", "中心频率偏移<br>Carrier deviation"},
        {"M.13", "LED显示<br>LED display"},
        {"M.14", "LCD显示<br>LCD display"},
        {"M.15", "实体按键<br>Physical button"},
        {"M.16", "触摸按键<br>Touch button"},
        {"M.17", "滚轮<br>Scroll wheel"},
        {"M.18", "通道切换与通道设置<br>Channel Switching and Channel Settings"},
        {"M.19", "群组切换与群组设置<br>Group Switching and Group Settings"},
        {"M.20", "群组内通道设置<br>Channel Settings in Group"},
        {"M.21", "定时设置<br>Timer Settings"},
        {"M.22", "充电功能<br>Charging function"},
        {"M.23", "单双向切换<br>OOK/FSK Switch"},
        {"M.24", "对码/删码/滚码<br>Code matching / Code deletion / Rolling code"},
        {"M.25", "群控控制<br>Group Control"},
        {"M.26", "换向功能<br>Reversing function"},
        {"M.27", "行程设置<br>Limit Settings"},
        {"M.28", "最佳行程设置<br>Favorite limit Settings"},
        {"M.29", "速度切换<br>Speed switch"},
        {"M.30", "百分比运行<br>Percentage Run"},
        {"M.31", "童锁<br>Lock"},
        {"M.32", "点动与续动切换<br>Jog and continuous mode switch"},
        {"M.33", "检测电机电池电量<br>Check motor battery level"},
        {"M.34", "检测电机位置<br>Detect motor position"},
        {"M.35", "掉电保存<br>Power-off preservation"},
        {"M.36", "定时发射功能<br>Scheduled launch function"},
        {"M.37", "客户特殊功能要求<br>Customer special feature requirements"},
        {"M.38", "远距离遥控测试<br>Long-distance remote control test"},
        {"M.39", "远距离接收测试<br>Long-distance reception test"},
        {"M.40", "恢复出厂设置<br>Factory reset"},
        {"M.41", "遥控器电量检测<br>Remote Control Battery Level Detection"},
        {"M.42", "USB、Type-C插拔测试<br>USB and Type-C Plug and Unplug Test"},
        {"M.43", "成品晃动测试<br>Finished Product Shake Test"},
        {"M.44", "配件<br>Accessories"}
    };

        public readonly Dictionary<string, string> _criteriaMap_Rechargeable_Bidirectional_Remote = new Dictionary<string, string>
    {
        {"M.1", "电路板焊接整齐、美观，元器件位置正确，线路板上无明显锡珠、焊渣<br>The circuit board is neatly and beautifully soldered, with correctly arranged components and no visible solder balls or residues on the board."},
        {"M.2", "整体外壳外观完好，色泽一致，无明显划伤、碰伤、敲击痕<br>The overall exterior casing is in good condition, with a consistent color and no obvious scratches, dents, or impact marks."},
        {"M.3", "LOGO图标清晰、准确，无残缺<br>The logo icon is clear, accurate, and intact."},
        {"M.4", "印刷清楚、正确，无明显倾斜、翘起现象<br>Clearly and correctly printed, without obvious tilting or lifting."},
        {"M.5", "参照说明书<br>Refer to the manual"},
        {"M.6", "不少打螺钉，每个螺钉都拧到位<br>No missing screws, each screw is fastened securely."},
        {"M.7", "参照规格书<br>Refer to the specifications"},
        {"M.8", "≤15uA"},
        {"M.9", "≤10S"},
        {"M.10", "参照规格书供电出现低电量提示<br>Low battery warning appears when powered according to the specification"},
        {"M.11", "10CM≥-30DB"},
        {"M.12", "±100KHz"},
        {"M.13", "亮度均匀，提示内容都正确<br>The brightness is even, and all the prompts are correct."},
        {"M.14", "显示清晰，提示内容都正确<br>The display is clear, and the prompts are all correct."},
        {"M.15", "所有按键功能正常，高度一致，手感一致，回弹有力，无卡死<br>All keys function normally, have consistent height and feel, with strong rebound and no sticking."},
        {"M.16", "触摸交互提示正常，相邻按键互不影响<br>Touch interaction prompts are normal, and adjacent keys do not affect each other."},
        {"M.17", "滚轮滑动或按下无卡死现象<br>No freezing when scrolling or pressing the wheel"},
        {"M.18", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.19", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.20", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.21", "设置时间、星期正常<br>Set the time and weekday normally"},
        {"M.22", "充电电流大于等于0.5C，充电提示显示正常<br>Charging current is ≥ 0.5C, and the charging indicator displays normally."},
        {"M.23", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.24", "正常添码删码，有滚码<br>Normal code addition and deletion, with rolling code function"},
        {"M.25", "每个通道电机都能被群控控制<br>Each channel motor can be controlled by group control."},
        {"M.26", "有无行程分两种操作（参照规格书）<br>Two operations with/without limits (refer to the specification)"},
        {"M.27", "上下行程以及行程调整、行程删除操作正常（参照规格书）<br>Setting upper/lower limits, adjustment and deletion operate normally (refer to the specification)."},
        {"M.28", "设置、删除、运行操作正常（参照规格书）<br>Settings, deletion, and operation run normally (refer to the specification)"},
        {"M.29", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.30", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.31", "参照规格书能够及时刷新图标，组合键功能操作取消<br>Icons can be refreshed in a timely manner according to the specifications, and the combination key function is disabled."},
        {"M.32", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.33", "参照规格书提示电机的电量<br>Check the motor's power according to the specification manual"},
        {"M.34", "参照规格书提示电机的位置<br>Refer to the specifications to locate the motor"},
        {"M.35", "设置好的通道数、群组数、定时数据等相应数据在遥控器彻底断电后皆能保存<br>The set number of channels, groups, scheduled data and other settings can all be retained after the remote control is completely powered off."},
        {"M.36", "参照规格书能够正常按照设定时间运行<br>It can operate normally according to the set time as specified in the specification."},
        {"M.37", "请备注具体功能，若无填NA<br>Please specify the exact function; if none, fill in NA."},
        {"M.38", "室内≥40m<br>Indoor ≥40m"},
        {"M.39", "室内≥20m<br>Indoor ≥20m"},
        {"M.40", "参照规格书操作恢复出厂<br>Factory reset per specification"},
        {"M.41", "电池图标提示正常<br>Battery icon indicates normal"},
        {"M.42", "反复插拔20次，要求样品充电功能正常<br>Plug and unplug 20 times, the sample's charging function works normally."},
        {"M.43", "手持样品进行摇晃5次，要求样品不能出现异响或者内部零件有窜动现象<br>Hold the sample and shake it 5 times, no abnormal noises and no internal component movement."},
        {"M.44", "配件齐全，外观良好<br>Complete accessories with good appearance"}
    };

        public readonly Dictionary<string, string> _fillingtype_Rechargeable_Bidirectional_Remote = new Dictionary<string, string>
{
{"M.1", "1"},
    {"M.2", "1"},
    {"M.3", "1"},
    {"M.4", "1"},
    {"M.5", "0"},
    {"M.6", "1"},
    {"M.7", "0"},
    {"M.8", "0"},
    {"M.9", "1"},
    {"M.10", "1"},
    {"M.11", "0"},
    {"M.12", "1"},
    {"M.13", "1"},
    {"M.14", "1"},
    {"M.15", "1"},
    {"M.16", "1"},
    {"M.17", "1"},
    {"M.18", "1"},
    {"M.19", "1"},
    {"M.20", "1"},
    {"M.21", "1"},
    {"M.22", "0"},
    {"M.23", "1"},
    {"M.24", "1"},
    {"M.25", "1"},
    {"M.26", "1"},
    {"M.27", "1"},
    {"M.28", "1"},
    {"M.29", "1"},
    {"M.30", "1"},
    {"M.31", "1"},
    {"M.32", "1"},
    {"M.33", "1"},
    {"M.34", "1"},
    {"M.35", "1"},
    {"M.36", "1"},
    {"M.37", "0"},
    {"M.38", "0"},
    {"M.39", "0"},
    {"M.40", "1"},
    {"M.41", "1"},
    {"M.42", "1"},
    {"M.43", "1"},
    {"M.44", "0"},

};
        /// <summary>
        /// 充电款单向遥控器
        /// </summary>
        public readonly Dictionary<string, string> _specificationDescMap_Rechargeable_Unidirectional_Remote = new Dictionary<string, string>
    {
        {"M.1", "线路板外观<br>Circuit board appearance"},
        {"M.2", "整机外观<br>Overall appearance of the product"},
        {"M.3", "LOGO<br>LOGO"},
        {"M.4", "标签铭牌<br>Label"},
        {"M.5", "重量<br>Weight"},
        {"M.6", "螺钉<br>Screw"},
        {"M.7", "发射电流（mA）<br>Emission Current (mA)"},
        {"M.8", "待机电流（UA）<br>Standby Current (UA)"},
        {"M.9", "进入休眠时间<br>Enter sleep mode"},
        {"M.10", "低电量提示<br>Low battery warning"},
        {"M.11", "发射强度<br>Emission intensity"},
        {"M.12", "中心频率偏移<br>Carrier deviation"},
        {"M.13", "LED显示<br>LED display"},
        {"M.14", "LCD显示<br>LCD display"},
        {"M.15", "实体按键<br>Physical button"},
        {"M.16", "触摸按键<br>Touch button"},
        {"M.17", "滚轮<br>Scroll wheel"},
        {"M.18", "通道切换与通道设置<br>Channel Switching and Channel Settings"},
        {"M.19", "群组切换与群组设置<br>Group Switching and Group Settings"},
        {"M.20", "群组内通道设置<br>Channel Settings in Group"},
        {"M.21", "定时设置<br>Timer Settings"},
        {"M.22", "充电功能<br>Charging function"},
        {"M.23", "对码/删码/滚码<br>Code matching / Code deletion / Rolling code"},
        {"M.24", "群控控制<br>Group Control"},
        {"M.25", "换向功能<br>Reversing function"},
        {"M.26", "行程设置<br>Limit Settings"},
        {"M.27", "最佳行程设置<br>Favorite limit Settings"},
        {"M.28", "速度切换<br>Speed switch"},
        {"M.29", "百分比运行<br>Percentage Run"},
        {"M.30", "童锁<br>Lock"},
        {"M.31", "点动与续动切换<br>Jog and continuous mode switch"},
        {"M.32", "掉电保存<br>Power-off preservation"},
        {"M.33", "定时发射功能<br>Scheduled launch function"},
        {"M.34", "客户特殊功能要求<br>Customer special feature requirements"},
        {"M.35", "远距离遥控测试<br>Long-distance remote control test"},
        {"M.36", "恢复出厂设置<br>Factory reset"},
        {"M.37", "遥控器电量检测<br>Remote Control Battery Level Detection"},
        {"M.38", "USB、Type-C插拔测试<br>USB and Type-C Plug and Unplug Test"},
        {"M.39", "成品晃动测试<br>Finished Product Shake Test"},
        {"M.40", "配件<br>Accessories"}
    };
        public readonly Dictionary<string, string> _criteriaMap_Rechargeable_Unidirectional_Remote = new Dictionary<string, string>
    {
        {"M.1", "电路板焊接整齐、美观，元器件位置正确，线路板上无明显锡珠、焊渣<br>The circuit board is neatly and beautifully soldered, with correctly arranged components and no visible solder balls or residues on the board."},
        {"M.2", "整体外壳外观完好，色泽一致，无明显划伤、碰伤、敲击痕<br>The overall exterior casing is in good condition, with a consistent color and no obvious scratches, dents, or impact marks."},
        {"M.3", "LOGO图标清晰、准确，无残缺<br>The logo icon is clear, accurate, and intact."},
        {"M.4", "印刷清楚、正确，无明显倾斜、翘起现象<br>Clearly and correctly printed, without obvious tilting or lifting."},
        {"M.5", "参照说明书<br>Refer to the manual"},
        {"M.6", "不少打螺钉，每个螺钉都拧到位<br>No missing screws, each screw is fastened securely."},
        {"M.7", "参照规格书<br>Refer to the specifications"},
        {"M.8", "≤15uA"},
        {"M.9", "≤10S"},
        {"M.10", "参照规格书供电出现低电量提示<br>Low battery warning appears when powered according to the specification"},
        {"M.11", "10CM≥-30DB"},
        {"M.12", "±100KHz"},
        {"M.13", "亮度均匀，提示内容都正确<br>The brightness is even, and all the prompts are correct."},
        {"M.14", "显示清晰，提示内容都正确<br>The display is clear, and the prompts are all correct."},
        {"M.15", "所有按键功能正常，高度一致，手感一致，回弹有力，无卡死<br>All keys function normally, have consistent height and feel, with strong rebound and no sticking."},
        {"M.16", "触摸交互提示正常，相邻按键互不影响<br>Touch interaction prompts are normal, and adjacent keys do not affect each other."},
        {"M.17", "滚轮滑动或按下无卡死现象<br>No freezing when scrolling or pressing the wheel"},
        {"M.18", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.19", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.20", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.21", "设置时间、星期正常<br>Set the time and weekday normally"},
        {"M.22", "充电电流大于等于0.5C，充电提示显示正常<br>Charging current is ≥ 0.5C, and the charging indicator displays normally."},
        {"M.23", "正常添码删码，有滚码<br>Normal code addition and deletion, with rolling code function"},
        {"M.24", "每个通道电机都能被群控控制<br>Each channel motor can be controlled by group control."},
        {"M.25", "有无行程分两种操作（参照规格书）<br>Two operations with/without limits (refer to the specification)"},
        {"M.26", "上下行程以及行程调整、行程删除操作正常（参照规格书）<br>Setting upper/lower limits, adjustment and deletion operate normally (refer to the specification)."},
        {"M.27", "设置、删除、运行操作正常（参照规格书）<br>Settings, deletion, and operation run normally (refer to the specification)"},
        {"M.28", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.29", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.30", "参照规格书能够及时刷新图标，组合键功能操作取消<br>Icons can be refreshed in a timely manner according to the specifications, and the combination key function is disabled."},
        {"M.31", "参照规格书操作正常<br>Operating normally according to the specifications"},
        {"M.32", "设置好的通道数、群组数、定时数据等相应数据在遥控器彻底断电后皆能保存<br>The set number of channels, groups, scheduled data and other settings can all be retained after the remote control is completely powered off."},
        {"M.33", "参照规格书能够正常按照设定时间运行<br>It can operate normally according to the set time as specified in the specification."},
        {"M.34", "请备注具体功能，若无填NA<br>Please specify the exact function; if none, fill in NA."},
        {"M.35", "室内≥40m<br>Indoor ≥40m"},
        {"M.36", "参照规格书操作恢复出厂<br>Factory reset per specification"},
        {"M.37", "电池图标提示正常<br>Battery icon indicates normal"},
        {"M.38", "反复插拔20次，要求样品充电功能正常<br>Plug and unplug 20 times, the sample's charging function works normally."},
        {"M.39", "手持样品进行摇晃5次，要求样品不能出现异响或者内部零件有窜动现象<br>Hold the sample and shake it 5 times, no abnormal noises and no internal component movement."},
        {"M.40", "配件齐全，外观良好<br>Complete accessories with good appearance"}
    };
        public readonly Dictionary<string, string> _fillingtype_Rechargeable_Unidirectional_Remote = new Dictionary<string, string>
{
   {"M.1", "1"},
    {"M.2", "1"},
    {"M.3", "1"},
    {"M.4", "1"},
    {"M.5", "0"},
    {"M.6", "1"},
    {"M.7", "0"},
    {"M.8", "0"},
    {"M.9", "1"},
    {"M.10", "1"},
    {"M.11", "0"},
    {"M.12", "1"},
    {"M.13", "1"},
    {"M.14", "1"},
    {"M.15", "1"},
    {"M.16", "1"},
    {"M.17", "1"},
    {"M.18", "1"},
    {"M.19", "1"},
    {"M.20", "1"},
    {"M.21", "1"},
    {"M.22", "0"},
    {"M.23", "1"},
    {"M.24", "1"},
    {"M.25", "1"},
    {"M.26", "1"},
    {"M.27", "1"},
    {"M.28", "1"},
    {"M.29", "1"},
    {"M.30", "1"},
    {"M.31", "1"},
    {"M.32", "1"},
    {"M.33", "1"},
    {"M.34", "0"},
    {"M.35", "0"},
    {"M.36", "1"},
    {"M.37", "1"},
    {"M.38", "1"},
    {"M.39", "1"},
    {"M.40", "1"},
    


};
        /// <summary>
        /// 单推杆遮阳棚
        /// </summary>
        public readonly Dictionary<string, string> _specificationDescMap_Single_PushRodAwning = new Dictionary<string, string>
{
    {"M.1", "线路板外观<br>Circuit board appearance"},
    {"M.2", "控制盒外观<br>Control box appearance"},
    {"M.3", "标签铭牌<br>Label"},
    {"M.4", "控制盒尺寸<br>Control box size"},
    {"M.5", "对码、删码<br>Pairing code, delete code"},
    {"M.6", "推杆消除异常状态<br>The push rod eliminates abnormal states"},
    {"M.7", "推杆自动设置行程<br>Automatic stroke setting for the push rod"},
    {"M.8", "推杆分别设置行程<br>Set the stroke of the push rod separately"},
    {"M.9", "推杆手动设置最佳行程<br>Manually set the optimal stroke for the pusher"},
    {"M.10", "推杆滚轮测试<br>Putter roller test"},
    {"M.11", "推杆运行测试<br>Push Rod Operation Test"},
    {"M.12", "推杆满载运行测试（灯带输出满载情况下）<br>Full-load operation test of the push rod (under full-load output of the light strip)"},
    {"M.13", "推杆过流保护测试<br>Putter Overcurrent Protection Test"},
    {"M.14", "RGB灯带<br>RGB light strip"},
    {"M.15", "RGB灯带记忆颜色设置<br>RGB light strip memory color setting"},
    {"M.16", "RGB灯带自动变色模式<br>RGB light strip automatic color-changing mode"},
    {"M.17", "RGB灯带满载测试<br>RGB LED strip full load test"},
    {"M.18", "RGB灯带短路保护测试<br>RGB LED Strip Short Circuit Protection Test"},
    {"M.19", "LED灯带<br>LED Light Strip"},
    {"M.20", "LED灯带满载测试<br>LED strip full load test"},
    {"M.21", "LED灯带短路保护测试<br>LED Strip Short Circuit Protection Test"},
    {"M.22", "温感测试<br>Temperature Sensitivity Test"},
    {"M.23", "雨感测试<br>Rain Sensor Test"},
    {"M.24", "风光测试<br>Scenery Test"},
    {"M.25", "低电量报警<br>Low Battery Alarm"},
    {"M.26", "客户特殊功能要求<br>Customer Special Function Requirements"},
    {"M.27", "断电测试<br>Power outage test"},
    {"M.28", "接收灵敏度<br>Reception sensitivity"},
    {"M.29", "连接线<br>Power cable"},
    {"M.30", "控制盒配件<br>Controller Box Accessories"},
    {"M.31", "成品晃动测试<br>Finished Product Shake Test"},
    {"M.32", "恢复出厂设置<br>Factory reset"}
};

        public readonly Dictionary<string, string> _criteriaMap_Single_PushRodAwning = new Dictionary<string, string>
{
    {"M.1", "电路板焊接整齐、美观，元器件位置正确，线路板上无明显珠，焊渣<br>The circuit board is neatly and beautifully soldered, with correctly arranged components and no visible solder balls or residues on the board."},
    {"M.2", "整体外壳外观完好，色泽一致，无明显划伤、碰伤、敲击痕<br>The overall appearance of the exterior casing is intact, with a consistent color and no obvious scratches, dents, or impact marks."},
    {"M.3", "印刷清楚、正确，无明显倾斜、翘起现象<br>Clearly and correctly printed, without obvious tilting or lifting."},
    {"M.4", "参照规格书<br>Refer to the specifications"},
    {"M.5", "正常添码删码（参照规格书）<br>Normal code addition and deletion (refer to the specification)"},
    {"M.6", "点动状态下，按住对应通道下键，推杆能够下降到底并回弹，恢复续动状态<br>In jog mode, holding down the lower button of the corresponding channel allows the joystick to move down to the bottom and then return, restoring continuous movement mode."},
    {"M.7", "上下行程以及行程调整、行程删除三种（参照规格书）<br>There are three types: upward and downward travel, travel adjustment, and travel deletion (refer to the specification)."},
    {"M.8", "上下行程以及行程调整、行程删除三种（参照规格书）<br>There are three types: upward and downward travel, travel adjustment, and travel deletion (refer to the specification)."},
    {"M.9", "设置、删除、运行三种情况（参照规格书）<br>Three scenarios: setting, deleting, and running (refer to the specifications)"},
    {"M.10", "对应推杆通道上每拨动一格，推杆持续上升/下降3-4mm<br>For each notch moved on the corresponding push rod channel, the push rod continues to rise/fall by 3-4mm."},
    {"M.11", "推杆群控通道上控制双推杆一起上行和下行，每根推杆都能运行到对应推杆的上行程点和下行程点。<br>On the push-rod group control channel, two push rods are controlled to move up and down together, and each push rod can operate to its corresponding upper and lower travel points."},
    {"M.12", "推杆群控通道上控制双推杆一起上行和下行，每根推杆都能运行到对应推杆的上行程点和下行程点。<br>On the push-rod group control channel, two push rods are controlled to move up and down together, and each push rod can operate to its corresponding upper and lower travel points."},
    {"M.13", "使用过流检测检具，保护值参考规格书。<br>Use the overcurrent detection fixture, and refer to the specifications for the protection value."},
    {"M.14", "打开，关闭，按键调光，旋转调光<br>Turn on, turn off, key dimming, rotary dimming."},
    {"M.15", "参照规格书能够更改记忆颜色<br>Memory color can be changed according to the specifications."},
    {"M.16", "参照规格书<br>Refer to the specifications"},
    {"M.17", "接LED&RGB测试工装，显示电流值大于等于额定电流。<br>Connect to the LED & RGB test fixture; the displayed current value is greater than or equal to the rated current."},
    {"M.18", "接LED&RGB测试工装，短路RGB灯带接口，再断开短路源后要求控制器正常。<br>Connect to the LED & RGB test fixture, short-circuit the RGB light strip interface; the controller shall function normally after the short-circuit source is removed."},
    {"M.19", "打开，关闭，按键调光，旋转调光<br>Turn on, turn off, key dimming, rotary dimming"},
    {"M.20", "接LED&RGB测试工装，显示电流值大于等于额定电流。<br>Connect to the LED & RGB test fixture; the displayed current value is greater than or equal to the rated current."},
    {"M.21", "接LED&RGB测试工装，短路LED灯带接口，再断开短路源后要求控制器正常。<br>Connect the LED & RGB test fixture, short-circuit the LED light strip interface. The controller shall work normally after removing the short-circuit source."},
    {"M.22", "默认模式，模式的打开和关闭，功能测试参照规格书<br>Default mode, turning the mode on and off, functional tests refer to the specifications."},
    {"M.23", "参照规格书<br>Refer to the specifications"},
    {"M.24", "参照规格书<br>Refer to the specifications"},
    {"M.25", "参照规格书<br>Refer to the specifications"},
    {"M.26", "请备注具体功能<br>Please specify the specific function."},
    {"M.27", "断电上电后检查行程点，发射器，传感器，推杆模式及温感模式。<br>After power off and on, check the travel points, transmitter, sensor, actuator mode, and temperature sensing mode."},
    {"M.28", "室内≥40m<br>Indoor ≥40m"},
    {"M.29", "接口型号，防水盖，防水标签，线序等<br>Interface model, waterproof cover, waterproof label, wire sequence, etc."},
    {"M.30", "参考样品需求<br>Reference sample requirements"},
    {"M.31", "手持样品进行摇晃5次，要求样品不能出现异响或者内部零件有窜动现象<br>Hold the sample and shake it 5 times, ensuring that no abnormal noises occur and that none of the internal components move."},
    {"M.32", "参照规格书操作恢复出厂<br>Factory reset per specification"}
};
        public readonly Dictionary<string, string> _fillingtype_Single_PushRodAwning = new Dictionary<string, string>
{
   {"M.1", "1"},
    {"M.2", "1"},
    {"M.3", "1"},
    {"M.4", "0"},
    {"M.5", "1"},
    {"M.6", "1"},
    {"M.7", "1"},
    {"M.8", "1"},
    {"M.9", "1"},
    {"M.10", "1"},
    {"M.11", "1"},
    {"M.12", "1"},
    {"M.13", "1"},
    {"M.14", "1"},
    {"M.15", "1"},
    {"M.16", "1"},
    {"M.17", "1"},
    {"M.18", "1"},
    {"M.19", "1"},
    {"M.20", "1"},
    {"M.21", "1"},
    {"M.22", "1"},
    {"M.23", "1"},
    {"M.24", "1"},
    {"M.25", "1"},
    {"M.26", "0"},
    {"M.27", "1"},
    {"M.28", "0"},
    {"M.29", "0"},
    {"M.30", "1"},
    {"M.31", "1"},
    {"M.32", "1"}
    
    };
        /// <summary>
        /// 双推杆遮阳棚
        /// </summary>

        public readonly Dictionary<string, string> _specificationDescMap_Dual_PushRodAwning = new Dictionary<string, string>
{
    {"M.1", "线路板外观<br>Circuit board appearance"},
    {"M.2", "控制盒外观<br>Control box appearance"},
    {"M.3", "标签铭牌<br>Label"},
    {"M.4", "控制盒尺寸<br>Control box size"},
    {"M.5", "对码、删码<br>Pairing motor with controller; Remove controller"},
    {"M.6", "单双推杆模式切换<br>Single/Double actuators Mode Switching"},
    {"M.7", "双推杆分别消除异常状态<br>The two push rods eliminate abnormal states separately"},
    {"M.8", "双推杆一起消除异常状态<br>Eliminate abnormal states with both push rods together"},
    {"M.9", "双推杆分别自动设置行程<br>The two push rods automatically set their stroke separately."},
    {"M.10", "双推杆一起自动设置行程<br>The two push rods automatically set their stroke separately."},
    {"M.11", "双推杆分别手动设置行程<br>The two push rods are manually set for travel separately."},
    {"M.12", "双推杆分别手动设置最佳行程<br>Manually set the optimal stroke for each of the two push rods"},
    {"M.13", "双推杆一起手动设置最佳行程<br>Manually set the optimal stroke with both push rods together"},
    {"M.14", "双推杆分别滚轮测试<br>Dual push rod roller testing"},
    {"M.15", "双推杆一起滚轮测试<br>Dual push-rod roller test"},
    {"M.16", "双推杆一起联动运行测试<br>Dual push rods linked operation test"},
    {"M.17", "双推杆一起满载运行测试（灯带输出满载情况下）<br>Dual push rods full-load operation test (with light strip output at full load)"},
    {"M.18", "双推杆分别过流保护测试<br>Dual push-rod overcurrent protection test"},
    {"M.19", "RGB灯带<br>RGB light strip"},
    {"M.20", "RGB灯带记忆颜色设置<br>RGB LED Strip Memory Color Settings"},
    {"M.21", "RGB灯带自动变色模式<br>RGB LED strip automatic color-changing mode"},
    {"M.22", "RGB灯带满载测试<br>RGB LED strip full load test"},
    {"M.23", "RGB灯带短路保护测试<br>RGB LED Strip Short Circuit Protection Test"},
    {"M.24", "LED灯带<br>LED Light Strip"},
    {"M.25", "LED灯带满载测试<br>LED strip full load test"},
    {"M.26", "LED灯带短路保护测试<br>LED Strip Short Circuit Protection Test"},
    {"M.27", "温感测试<br>Temperature Sensitivity Test"},
    {"M.28", "雨感测试<br>Rain Sensor Test"},
    {"M.29", "风光测试<br>Scenery Test"},
    {"M.30", "低电量报警<br>Low Battery Alarm"},
    {"M.31", "客户特殊功能要求<br>Customer special function requirements"},
    {"M.32", "断电测试<br>Power outage test"},
    {"M.33", "接收灵敏度<br>Reception sensitivity"},
    {"M.34", "连接线<br>Power cable"},
    {"M.35", "控制盒配件<br>Controller Box Accessories"},
    {"M.36", "成品晃动测试<br>Finished Product Shake Test"},
    {"M.37", "恢复出厂设置<br>Factory reset"}
};
        public readonly Dictionary<string, string> _criteriaMap_Dual_PushRodAwning = new Dictionary<string, string>
{
    {"M.1", "电路板焊接整齐、美观，元器件位置正确，线路板上无明显珠，焊渣<br>The circuit board is neatly and beautifully soldered, with correctly arranged components and no visible solder balls or residues on the board."},
    {"M.2", "整体外壳外观完好，色泽一致，无明显划伤、碰伤、敲击痕<br>The overall appearance of the exterior casing is intact, with a consistent color and no obvious scratches, dents, or impact marks."},
    {"M.3", "印刷清楚、正确，无明显倾斜、翘起现象<br>Clearly and correctly printed, without obvious tilting or lifting."},
    {"M.4", "参照规格书<br>Refer to the specifications"},
    {"M.5", "正常添码删码（参照规格书）<br>Normal pairing for motor with controller and Removing controller (refer to the specification)"},
    {"M.6", "参照规格书<br>Refer to the specifications"},
    {"M.7", "点动状态下，按住对应通道下键，推杆能够下降到底并回弹，恢复续动状态<br>In jog mode, pressing and holding the corresponding channel's down button allows the joystick to move all the way down and then return, restoring the continuous motion state."},
    {"M.8", "点动状态下，按住对应通道下键，推杆能够下降到底并回弹，恢复续动状态<br>In jog mode, pressing and holding the corresponding channel's down button allows the joystick to move all the way down and then return, restoring the continuous motion state."},
    {"M.9", "上下行程以及行程调整、行程删除三种（参照规格书）<br>There are three types: Setting upper limits and lower limits, as well as limits adjustment and limits deletion (refer to the specification)."},
    {"M.10", "上下行程以及行程调整、行程删除三种（参照规格书）<br>There are three types: Setting upper limits and lower limits, as well as limits adjustment and limits deletion (refer to the specification)."},
    {"M.11", "上下行程以及行程调整、行程删除三种（参照规格书）<br>There are three types: Setting upper limits and lower limits, as well as limits adjustment and limits deletion (refer to the specification)."},
    {"M.12", "设置、删除、运行三种情况（参照规格书）<br>Three scenarios: setting, deleting, and running (refer to the specifications)"},
    {"M.13", "设置、删除、运行三种情况（参照规格书）<br>Three scenarios: setting, deleting, and running (refer to the specifications)"},
    {"M.14", "对应推杆通道上每拨动一格，推杆持续上升/下降3-4mm<br>For each notch moved on the corresponding push rod channel, the push rod continues to rise/fall by 3-4mm."},
    {"M.15", "对应推杆通道上每拨动一格，推杆持续上升/下降3-4mm<br>For each notch moved on the corresponding push rod channel, the push rod continues to rise/fall by 3-4mm."},
    {"M.16", "推杆群控通道上控制双推杆一起上行和下行，每根推杆都能运行到对应推杆的上行程点和下行程点。<br>On the push-rod group control channel, two push rods are controlled to move up and down together, and each push rod can operate to its corresponding upper and lower travel points."},
    {"M.17", "推杆群控通道上控制双推杆一起上行和下行，每根推杆都能运行到对应推杆的上行程点和下行程点。<br>On the push-rod group control channel, two push rods are controlled to move up and down together, and each push rod can operate to its corresponding upper and lower travel points."},
    {"M.18", "使用过流检测检具，保护值参考规格书。<br>Use the overcurrent detection fixture, and refer to the specifications for the protection value."},
    {"M.19", "打开，关闭，按键调光，旋转调光<br>Turn on, turn off, key dimming, rotary dimming."},
    {"M.20", "参照规格书能够更改记忆颜色<br>Memory colors can be modified according to the specifications."},
    {"M.21", "参照规格书<br>Refer to the specifications"},
    {"M.22", "接LED&RGB测试工装，显示电流值大于等于额定电流。<br>Connect to the LED & RGB test fixture; the displayed current value is greater than or equal to the rated current."},
    {"M.23", "接LED&RGB测试工装，短路RGB灯带接口，再断开短路源后要求控制器正常。<br>Connect to the LED & RGB test fixture, short-circuit the RGB light strip interface; the controller shall function normally after the short-circuit source is removed."},
    {"M.24", "打开，关闭，按键调光，旋转调光<br>Turn on, turn off, key dimming, rotary dimming."},
    {"M.25", "接LED&RGB测试工装，显示电流值大于等于额定电流。<br>Connect to the LED & RGB test fixture; the displayed current value is greater than or equal to the rated current."},
    {"M.26", "接LED&RGB测试工装，短路LED灯带接口，再断开短路源后要求控制器正常。<br>Connect the LED & RGB test fixture, short-circuit the LED light strip interface. The controller shall work normally after removing the short-circuit source."},
    {"M.27", "默认模式，模式的打开和关闭，功能测试参照规格书<br>Default mode, turning the mode on and off, functional tests refer to the specifications."},
    {"M.28", "参照规格书<br>Refer to the specifications"},
    {"M.29", "参照规格书<br>Refer to the specifications"},
    {"M.30", "参照规格书<br>Refer to the specifications"},
    {"M.31", "请备注具体功能<br>Please note the specific function"},
    {"M.32", "断电上电后检查行程点，发射器，传感器，推杆模式及温感模式。<br>After power cycling, check the travel points, transmitter, sensor, push rod mode, and temperature sensing mode."},
    {"M.33", "室内≥40m<br>Indoor ≥40m"},
    {"M.34", "接口型号，防水盖，防水标签，线序等<br>Interface model, waterproof cap, waterproof label, wire sequence, etc."},
    {"M.35", "参考样品需求<br>Reference sample requirements"},
    {"M.36", "手持样品进行摇晃5次，要求样品不能出现异响或者内部零件有窜动现象<br>Hold the sample and shake it 5 times, ensuring that no abnormal noises occur and that none of the internal components move."},
    {"M.37", "参照规格书操作恢复出厂<br>Factory reset per specification"}
};
        public readonly Dictionary<string, string> _fillingtype_Dual_PushRodAwning = new Dictionary<string, string>
{
   {"M.1", "1"},
    {"M.2", "1"},
    {"M.3", "1"},
    {"M.4", "0"},
    {"M.5", "1"},
    {"M.6", "1"},
    {"M.7", "1"},
    {"M.8", "1"},
    {"M.9", "1"},
    {"M.10", "1"},
    {"M.11", "1"},
    {"M.12", "1"},
    {"M.13", "1"},
    {"M.14", "1"},
    {"M.15", "1"},
    {"M.16", "1"},
    {"M.17", "1"},
    {"M.18", "0"},
    {"M.19", "1"},
    {"M.20", "1"},
    {"M.21", "1"},
    {"M.22", "1"},
    {"M.23", "1"},
    {"M.24", "1"},
    {"M.25", "1"},
    {"M.26", "1"},
    {"M.27", "1"},
    {"M.28", "1"},
    {"M.29", "1"},
    {"M.30", "1"},
    {"M.31", "0"},
    {"M.32", "1"},
    {"M.33", "0"},
    {"M.34", "0"},
    {"M.35", "1"},
    {"M.36", "1"},
    {"M.37", "1"}};

        // 样品部件字典-遥控器，目前一致可共用
        public readonly Dictionary<string, string> _sampleComponents_Remote = new Dictionary<string, string>
{
    {"S.1", "部件1"},
    {"S.2", "部件2"},
    {"S.3", "部件3"},
    {"S.4", "部件4"},
    {"S.5", "部件5"},
    {"S.6", "部件6"},
};
        // 要求字典-遥控器，目前一致可共用
        public readonly Dictionary<string, string> _requirements_Remote = new Dictionary<string, string>
{
    {"S.1", ""},
    {"S.2", ""},
    {"S.3", ""},
    {"S.4", ""},
    {"S.5", ""},
    {"S.6", ""},
};
}
