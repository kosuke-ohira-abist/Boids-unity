## Boids

![GIF](https://github.com/kosuke-ohira-abist/Boids-unity/blob/main/boids.gif)

## 特長

* **GPGPU**
  
  Compute ShaderによるBoids計算の高速化
  
* **GPUインスタンシング描画**
  
  Compute ShaderによるBoids計算結果をCPUを介さず直接描画用シェーダーに渡し、GPUインスタンシング描画

* **空間分割による高速化**

  Boundsを空間分割し各オブジェクトのBoids計算を98%程度削減

## 動作確認

macbook pro 2019 13inchで、16384オブジェクトを生成し40FPS程度を確認済み

## 参照文献
[群のシミュレーションのGPU実装](https://github.com/IndieVisualLab/UnityGraphicsProgrammingBook1/blob/master/oishi.md)

