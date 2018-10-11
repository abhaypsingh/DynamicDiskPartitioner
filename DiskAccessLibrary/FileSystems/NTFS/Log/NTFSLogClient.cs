/* Copyright (C) 2018 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using Utilities;

namespace DiskAccessLibrary.FileSystems.NTFS
{
    public class NTFSLogClient
    {
        private const string ClientName = "NTFS";

        private LogFile m_logFile;
        private int m_clientIndex;

        public NTFSLogClient(NTFSVolume volume)
        {
            m_logFile = new LogFile(volume);
            m_clientIndex = m_logFile.FindClientIndex(ClientName);
            if (m_clientIndex == -1)
            {
                throw new InvalidDataException("NTFS Client was not found");
            }
        }

        public NTFSRestartRecord ReadRestartRecord()
        {
            ulong clientRestartLsn = m_logFile.GetClientRecord(m_clientIndex).ClientRestartLsn;
            LogRecord record = m_logFile.ReadRecord(clientRestartLsn);
            if (record.RecordType == LogRecordType.ClientRestart)
            {
                return new NTFSRestartRecord(record.Data);
            }
            else
            {
                string message = String.Format("Log restart area points to a record with RecordType {0}", record.RecordType);
                throw new InvalidDataException(message);
            }
        }

        public List<OpenAttributeEntry> ReadCurrentOpenAttributeTable()
        {
            NTFSRestartRecord restartRecord = ReadRestartRecord();
            ulong openAttributeTableLsn = restartRecord.OpenAttributeTableLsn;
            if (openAttributeTableLsn != 0)
            {
                NTFSLogRecord record = ReadLogRecord(openAttributeTableLsn);
                if (record.RedoOperation != NTFSLogOperation.OpenAttributeTableDump)
                {
                    string message = String.Format("Current restart record OpenAttributeTableLsn points to a record with RedoOperation {0}", record.RedoOperation);
                    throw new InvalidDataException(message);
                }

                if (restartRecord.OpenAttributeTableLength != record.RedoData.Length)
                {
                    throw new InvalidDataException("Open attribute table length does not match restart record");
                }

                return RestartTableHelper.ReadTable<OpenAttributeEntry>(record.RedoData, restartRecord.MajorVersion);
            }
            else
            {
                return null;
            }
        }

        public List<DirtyPageEntry> ReadCurrentDirtyPageTable()
        {
            NTFSRestartRecord restartRecord = ReadRestartRecord();
            ulong dirtyPageTableLsn = restartRecord.DirtyPageTableLsn;
            if (dirtyPageTableLsn != 0)
            {
                NTFSLogRecord record = ReadLogRecord(dirtyPageTableLsn);
                if (record.RedoOperation != NTFSLogOperation.DirtyPageTableDump)
                {
                    string message = String.Format("Current restart record DirtyPageTableLsn points to a record with RedoOperation {0}", record.RedoOperation);
                    throw new InvalidDataException(message);
                }

                if (restartRecord.DirtyPageTableLength != record.RedoData.Length)
                {
                    throw new InvalidDataException("Dirty page table length does not match restart record");
                }

                return RestartTableHelper.ReadTable<DirtyPageEntry>(record.RedoData, restartRecord.MajorVersion);
            }
            else
            {
                return null;
            }
        }

        public List<TransactionEntry> ReadCurrentTransactionTable()
        {
            NTFSRestartRecord restartRecord = ReadRestartRecord();
            ulong transactionTableLsn = restartRecord.TransactionTableLsn;
            if (transactionTableLsn != 0)
            {
                NTFSLogRecord record = ReadLogRecord(transactionTableLsn);
                if (record.RedoOperation != NTFSLogOperation.TransactionTableDump)
                {
                    string message = String.Format("Current restart record TransactionTableLsn points to a record with RedoOperation {0}", record.RedoOperation);
                    throw new InvalidDataException(message);
                }

                if (restartRecord.TransactionTableLength != record.RedoData.Length)
                {
                    throw new InvalidDataException("Transcation table length does not match restart record");
                }

                return RestartTableHelper.ReadTable<TransactionEntry>(record.RedoData, restartRecord.MajorVersion);
            }
            else
            {
                return null;
            }
        }

        public NTFSLogRecord ReadLogRecord(ulong lsn)
        {
            LogRecord record = m_logFile.ReadRecord(lsn);
            if (record.RecordType == LogRecordType.ClientRecord)
            {
                return new NTFSLogRecord(record.Data);
            }
            else
            {
                return null;
            }
        }
    }
}