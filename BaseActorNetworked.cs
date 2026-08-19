
using CoreEngine.EventBus;
using CoreEngine.Hub;

namespace CoreEngine.Network
{
    // 멀티플레이 객체용 3계층 Leaf 기본 클래스

    public abstract class BaseActorNetworked : BaseLeafNetworked, IActor
    {
        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            // 이 시점에는 IsServer, OwnerId 등 네트워크 정보가 완벽히 세팅되어 있습니다.
            var evt = new ActorRegistrationEvent(this, true, myScope);
            EventBus<ActorRegistrationEvent>.Publish(evt);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            // Hub에 내가 안 쓰임을 알림
            var evt = new ActorRegistrationEvent(this, false, myScope);
            EventBus<ActorRegistrationEvent>.Publish(evt);
        }
    }
}
