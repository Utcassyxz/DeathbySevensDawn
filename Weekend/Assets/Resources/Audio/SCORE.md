# 原创原型配乐

80 BPM，3/4 拍，16 小节，36 秒循环；双声道 32 kHz PCM。

ApartmentHome：柔和拨弦、钟琴与低音。BehindTheFilter：共享节拍与主题轮廓，加入错位音程和轻微失谐。Revelation：5 秒低音和声事件音。全部由项目脚本合成，无第三方采样或既有游戏旋律。

两个循环采用环形混响尾音，播放从同一个 DSP 时间启动，跨场景保持同步。表中 RMS 是幅度指标，不是 LUFS。仍需在实际耳机和扬声器上做主观混音复核。

```json
{
  "ApartmentHome": {
    "seconds": 36.0,
    "sample_rate": 32000,
    "peak_dbfs": -11.76,
    "rms_dbfs": -25.8,
    "loop_boundary_delta": 0.0025947884820215414
  },
  "BehindTheFilter": {
    "seconds": 36.0,
    "sample_rate": 32000,
    "peak_dbfs": -14.7,
    "rms_dbfs": -27.42,
    "loop_boundary_delta": 0.0005652936796074967
  },
  "Revelation": {
    "seconds": 5.0,
    "sample_rate": 32000,
    "peak_dbfs": -19.29,
    "rms_dbfs": -31.99,
    "loop_boundary_delta": 2.5280911476026893e-08
  }
}
```
