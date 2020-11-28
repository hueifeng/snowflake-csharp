using System;

namespace Snowflake
{
    /// <summary>
    ///     twitter的snowflake算法 -- c#实现
    /// </summary>
    public class SnowFlake
    {
        /// <summary>
        /// 起始时间戳
        /// </summary>
        private const long StartStamp = 1480166465631L;

        /// <summary>
        /// 每一部分占用的位数
        /// </summary>
        private const int SequenceBit = 12; //序列号占用的位数
        private const int MachineBit = 5;   //机器标识占用的位数
        private const int DatacenterBit = 5;//数据中心占用的位数

        /// <summary>
        /// 每一部分的最大值
        /// </summary>
        private const int MaxDatacenterNum = -1 ^ (-1 << DatacenterBit);
        private const int MaxMachineNum = -1 ^ (-1 << MachineBit);
        private const int MaxSequence = -1 ^ (-1 << SequenceBit);

        /// <summary>
        /// 每一部分向左的位移
        /// </summary>
        private const int MachineLeft = SequenceBit;
        private const int DatacenterLeft = SequenceBit + MachineBit;
        private const int TimestampLeft = DatacenterLeft + DatacenterBit;

        private readonly long _datacenterId;  //数据中心
        private static long _machineId;     //机器标识
        private long _sequence; //序列号
        private long _lastStamp = -1L;//上一次时间戳
        public SnowFlake(long datacenterId, long machineId)
        {
            if (datacenterId > MaxDatacenterNum || datacenterId < 0)
            {
                throw new ArgumentException("datacenterId can't be greater than MAX_DATACENTER_NUM or less than 0");
            }
            if (machineId > MaxMachineNum || machineId < 0)
            {
                throw new ArgumentException("machineId can't be greater than MAX_MACHINE_NUM or less than 0");
            }
            _datacenterId = datacenterId;
            _machineId = machineId;
        }

        readonly object _lock = new object();

        /// <summary>
        ///     产生下一个ID
        /// </summary>
        /// <returns></returns>
        public virtual long NextId()
        {
            lock (_lock)
            {
                long currStamp = GetNewStamp();
                if (currStamp < _lastStamp)
                {
                    throw new Exception("Clock moved backwards.  Refusing to generate id");
                }

                if (currStamp == _lastStamp)
                {
                    //相同毫秒内，序列号自增
                    _sequence = (_sequence + 1) & MaxSequence;
                    //同一毫秒的序列数已经达到最大
                    if (_sequence == 0L)
                    {
                        currStamp = GetNextMill();
                    }
                }
                else
                {
                    //不同毫秒内，序列号置为0
                    _sequence = 0;
                }

                _lastStamp = currStamp;

                var id= ((currStamp - StartStamp) << TimestampLeft) //时间戳部分
                       | (_datacenterId << DatacenterLeft) //数据中心部分
                       | (_machineId << MachineLeft) //机器标识部分
                       | _sequence; //序列号部分

                return id;
            }
        }

        private long GetNextMill()
        {
            long mill = GetNewStamp();
            while (mill <= _lastStamp)
            {
                mill = GetNewStamp();
            }
            return mill;
        }

        public static void SetMachineId(long machineId)
        {
            _machineId = machineId;
        }

        private long GetNewStamp()
        {
            return System.CurrentTimeMillis();
        }

    }
}
