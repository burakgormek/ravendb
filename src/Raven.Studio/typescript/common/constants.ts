const enum buildType {
    Stable = 0,
    Unstable = 1,
}

const enum viewType {
    Documents = 0,
    Counters = 1,
    TimeSeries = 2,
}
const enum ResponseCodes {
    Forbidden = 403,
    NotFound = 404,
    PreconditionFailed = 412,
    InternalServerError = 500,
    ServiceUnavailable = 503
}


const enum checkbox {
    UnChecked = 0,
    SomeChecked = 1,
    Checked = 2
}

const enum TenantType {
    Database = 0,
    FileSystem = 1,
    CounterStorage = 2,
    TimeSeries = 3
}

const enum ImportItemType {
    Documents = 0x1,
    Indexes = 0x2,
    Transformers = 0x8,
    RemoveAnalyzers = 0x8000
}

const enum filesystemSynchronizationType {
    Unknown = 0,
    ContentUpdate = 1,
    MetadataUpdate = 2,
    Rename = 3,
    Delete = 4,
}


const enum synchronizationAction {
    Enqueue,
    Start,
    Finish
}

const enum synchronizationDirection {
    Outgoing,
    Incoming
}

const enum conflictStatus {
    Detected = 0,
    Resolved = 1
}

const enum filesystemConfigurationChangeAction {
    Set,
    Delete
}