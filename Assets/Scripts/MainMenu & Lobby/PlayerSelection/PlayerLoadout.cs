using Unity.Netcode;

namespace MainMenu.PlayerSelection
{
    public struct PlayerLoadout: INetworkSerializable, System.IEquatable<PlayerLoadout>
    {
        public int CharacterId;
        public int FloatieId;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            
        }

        public bool Equals(PlayerLoadout other)
        {
            return false;
        }
    }
}