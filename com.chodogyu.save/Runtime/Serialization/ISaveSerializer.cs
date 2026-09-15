using CDG.Core.Results;

namespace CDG.Save.Serialization
{
    /// <summary>
    /// 저장 데이터를 바이트 배열로 직렬화하고 다시 원래 타입으로 복원하는 기능을 정의합니다.
    /// 파일 경로, 저장 슬롯, 저장 매체와 같은 영속성 책임은 포함하지 않습니다.
    /// </summary>
    public interface ISaveSerializer
    {
        /// <summary>
        /// 지정한 저장 데이터를 바이트 배열로 직렬화합니다.
        /// 직렬화할 수 없는 경우 실패 Result를 반환합니다.
        /// </summary>
        /// <typeparam name="T">직렬화할 저장 데이터의 타입입니다.</typeparam>
        /// <param name="data">직렬화할 저장 데이터입니다.</param>
        /// <returns>성공 시 직렬화된 바이트 배열을 포함하는 결과입니다.</returns>
        Result<byte[]> Serialize<T>(T data);

        /// <summary>
        /// 지정한 바이트 배열을 저장 데이터 타입으로 역직렬화합니다.
        /// 데이터를 복원할 수 없는 경우 실패 Result를 반환합니다.
        /// </summary>
        /// <typeparam name="T">복원할 저장 데이터의 타입입니다.</typeparam>
        /// <param name="data">역직렬화할 바이트 배열입니다.</param>
        /// <returns>성공 시 복원된 저장 데이터를 포함하는 결과입니다.</returns>
        Result<T> Deserialize<T>(byte[] data);
    }
}