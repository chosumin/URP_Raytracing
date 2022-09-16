**Ray Tracing**

기존의 렌더링 방식인 레스터라이징에 비하여 더욱 사실적인 반사 표현이 가능한 렌더링 기법

카메라가 보고 있는 방향부터 광원까지의 길을 역추적하여 렌더링하는 방법

**결과**

![image](https://user-images.githubusercontent.com/10754000/190593414-85b5984f-9b71-4f2b-beb6-0829f669fe58.png)

**구현 방법**

1. 카메라 방향의 최초 광선을 생성
![image](https://user-images.githubusercontent.com/10754000/190592619-816214ae-4d3b-4627-8e58-63d1ecf16579.png)

2. 광선이 오브젝트와 만나면서 반사 정보를 계산(색, 반사 위치, 반사각, 반사 거리)
![image](https://user-images.githubusercontent.com/10754000/190592950-957ef03e-3d64-4045-9e8e-6c9c05b03327.png)

3. 반사 정보를 바탕으로 Diffuse, Reflection, Shadow 된 최종 색을 구함
![image](https://user-images.githubusercontent.com/10754000/190593147-e59e3463-e40e-4461-834e-8afd673e6b21.png)
