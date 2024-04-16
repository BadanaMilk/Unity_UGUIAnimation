# Unity_UGUIAnimation
Unity UGUI Animation Component

UI용 애니메이션을 제작 및 사용하기 위한 컴포넌트

![image](https://github.com/BadanaMilk/Unity_UGUIAnimation/assets/49542105/064f834a-dcfe-4b4a-835a-a2f581b8a8aa)
(해당 컴포넌트를 추가한 Inspector의 모습과 표시된 버튼을 눌러서 설정 윈도우를 열었을 때의 타임라인 창)

![SampleVideo](https://github.com/BadanaMilk/Unity_UGUIAnimation/assets/49542105/f8552684-e149-4e32-a6e1-696c26fb22c3)

버튼을 통해 Editor상으로 재생 밑 역재생을 확인 할 수 있고 타임라인의 슬라이더 조작을 통해 해당 시간때에 모습을 미리 볼 수 있습니다. 


설명


그룹 (Group)

![image](https://github.com/BadanaMilk/Unity_UGUIAnimation/assets/49542105/f60af8c6-c01b-4efe-816b-e61ebeba0293)
- 애니메이션을 적용시킬 대상 오브젝트를 기준으로 애니메이션 트랙 및 클립들을 세팅하는 틀


트랙 (Track)

![image](https://github.com/BadanaMilk/Unity_UGUIAnimation/assets/49542105/0ed9ce50-d31d-400d-b5d3-3a9944d48454)
- 애니메이션에 적용될 효과 별로 트랙을 생성할 수 있으며 각 트랙은 클립들을 가지고 있다.
- 트랙을 추가하려면 그룹의 우상단 +버튼 혹은 트랙의 우상단 +버튼을 통해 추가할 수 있다.
- 트랙의 삭제는 삭제하려는 해당 트랙에 우클릭을 통해 삭제 할 수 있다.


클립 (Clip)

![image](https://github.com/BadanaMilk/Unity_UGUIAnimation/assets/49542105/a5d3700b-5f07-46b8-a9ad-67456eec185e)

<img width="286" alt="UIAnimation_ClipInspector" src="https://github.com/BadanaMilk/Unity_UGUIAnimation/assets/49542105/2ca4b02a-7dcf-49f6-8fbf-e16db9f187d5">

(윈도우에서 클립을 선택했을때 컴포넌트의 Inspector 모습)

- 적용될 애니메이션의 시작 시간과 끝나는 시간, 그리고 조작하는 값들을 가지고 있다.
- 클립의 추가는 원하는 트랙의 타임라인 우 클릭을 통해 추가 할 수 있다.
- 클립의 수정은 원하는 클립을 좌 클릭하면 해당 클립의 정보가 Inspector에 표시되고 수정 할 수 있다.
- 클립의 삭제는 대상 클립의 우 클릭을 통해 삭제 할 수 있다.
- 트랙의 종류 별 클립의 Inspector 설정 모습 :
  - Position : 일반적인 오브젝트 이동 타입
  - Position_Besir2D : 오브젝트가 해당 위치까지 움직일 때 곡선으로 이동하는 타입. Control Value 2개를 통해 곡선을 조정.
  - Active : 오브젝트의 활성화/비활성화 타입
  - Scale : 오브젝트의 스케일
  - Rotation : 오브젝트의 회전
  - Color : 오브젝트에 Graphics 컴포넌트가 있는 경우 색상을 변경하는 타입
  - Alpha : 오브젝트에 Canvas Group이 있거나 Graphics관련 컴포넌트가 있을 경우 알파값을 변경하는 타입
