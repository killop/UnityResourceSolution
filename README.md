# 青霜-Unity-Resource-Solution

## 描述
- URS是一套unity资源管理方案，该方案把原始资源看作为一等公民，更新系统和ab系统完全分离。该方案包含了资源的导入，ab的导出，ab的热更新。这套方案涉及到了你在日常unity资源管理中的有可能遇到的方方面面
- URS的方向和初衷：争取在各个问题领域给出最好的答案
- 青霜的名字取自（腾蛟起凤，孟学士之词宗；紫电青霜，王将军之武库 --滕王阁序）
## 特点
- 支持边玩边下载资源
- 完备的基于tag的资源管理系统，一个资源多个tag
- 按照目录结构更新
- 文件不以hash结尾
- 全网唯一支持二次打包的系统，根据第一次打包的结果，自动优化ab大小，io数量。在零冗余和减少IO之间取得一个适当的平衡，相信我，用了它你会起飞的
- 优化并且扩展了[smart-library](https://assetstore.unity.com/packages/tools/utilities/smart-library-asset-manager-200724) 打造了一个优美的打包资源收集系统
- 支持 AssetBundleBrowser
- 无状态多版本管理系统
- binary diff，保证多版本之间最小更新体积
- shader变体收集工具
- shader变体裁剪工具
- 动画任意长度裁剪工具
- 动画属性绑定失败纠正和报错工具
- 材质多余属性剔除工具
## 路线图
- 远程下载暂时用了比较稳健的besthttp插件，正在接入：https://github.com/bezzad/Downloader

## 参考
- [YooAsset](https://github.com/tuyoogame/YooAsset) URS运行时的代码，很大一部分来自 YooAsset

## 交流
- 如果遇到任何问题：进qq群讨论：1067123079       
## 免责声明
里面用的收费插件，仅仅用做学习的目的，鼓励大家购买正版，不要随便传播收费插件