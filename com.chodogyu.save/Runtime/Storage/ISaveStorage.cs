using CDG.Core.Results;

namespace CDG.Save.Storage
{
    /// <summary>
    /// 직렬화된 저장 데이터를 실제 저장 매체에 기록하고 읽기 위한 기능을 정의합니다.
    /// 데이터의 직렬화 형식이나 게임별 저장 데이터 타입은 알지 못합니다.
    /// </summary>
    public interface ISaveStorage
    {
        /// <summary>
        /// 지정한 슬롯과 저장 파일 종류에 바이트 데이터를 기록합니다.
        /// </summary>
        /// <param name="slot">데이터를 기록할 저장 슬롯입니다.</param>
        /// <param name="copy">기본 또는 백업 저장 파일 종류입니다.</param>
        /// <param name="data">기록할 직렬화된 바이트 데이터입니다.</param>
        /// <returns>기록 작업의 성공 또는 실패 결과입니다.</returns>
        Result Write(SaveSlot slot, SaveStorageCopy copy, byte[] data);

        /// <summary>
        /// 지정한 슬롯과 저장 파일 종류에서 바이트 데이터를 읽습니다.
        /// 파일이 존재하지 않는 경우 StorageNotFound 오류를 반환합니다.
        /// </summary>
        /// <param name="slot">데이터를 읽을 저장 슬롯입니다.</param>
        /// <param name="copy">기본 또는 백업 저장 파일 종류입니다.</param>
        /// <returns>성공 시 읽은 바이트 데이터를 포함하는 결과입니다.</returns>
        Result<byte[]> Read(SaveSlot slot, SaveStorageCopy copy);

        /// <summary>
        /// 지정한 슬롯과 저장 파일 종류의 데이터가 존재하는지 확인합니다.
        /// </summary>
        /// <param name="slot">존재 여부를 확인할 저장 슬롯입니다.</param>
        /// <param name="copy">기본 또는 백업 저장 파일 종류입니다.</param>
        /// <returns>성공 시 존재 여부를 포함하는 결과입니다.</returns>
        Result<bool> Exists(SaveSlot slot, SaveStorageCopy copy);

        /// <summary>
        /// 지정한 슬롯과 저장 파일 종류의 데이터를 삭제합니다.
        /// </summary>
        /// <param name="slot">삭제할 저장 슬롯입니다.</param>
        /// <param name="copy">기본 또는 백업 저장 파일 종류입니다.</param>
        /// <returns>삭제 작업의 성공 또는 실패 결과입니다.</returns>
        Result Delete(SaveSlot slot, SaveStorageCopy copy);
    }
}