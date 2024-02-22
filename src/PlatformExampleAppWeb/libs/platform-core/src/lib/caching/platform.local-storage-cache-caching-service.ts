import { task_debounce, toPlainObj } from '../utils';
import {
    DefaultPlatformCachingServiceOptions as defaultPlatformCachingServiceOptions,
    PlatformCachingItem,
    PlatformCachingService,
    PlatformCachingServiceOptions,
    PlatformCachingServiceSetCacheOptions
} from './platform.caching-service';

/**
 * Local storage caching service implementation.
 *
 * @remarks
 * This class extends the {@link PlatformCachingService} abstract class and provides a caching service
 * that utilizes the browser's local storage for storing cached data.
 *
 * @example
 * ```typescript
 * // Create an instance of PlatformLocalStorageCachingService
 * const localStorageCacheService = new PlatformLocalStorageCachingService();
 *
 * // Use caching methods such as get, set, delete, etc.
 * const cachedData = localStorageCacheService.get<MyData>('myDataCacheKey');
 * ```
 */
export class PlatformLocalStorageCachingService extends PlatformCachingService {
    protected cacheKey: string;
    protected cache: Map<string, PlatformCachingItem> = new Map();

    constructor(options?: PlatformCachingServiceOptions) {
        super(options ?? defaultPlatformCachingServiceOptions());
        this.cacheKey = '__PlatformLocalStorageCachingService__';
        this.loadCache();
    }

    /**
     * Loads cached data from local storage.
     */
    public loadCache() {
        const cachedData = localStorage.getItem(this.cacheKey);
        this.cache = cachedData != null ? new Map(JSON.parse(cachedData)) : new Map();
        this.removeExpiredItems();
    }

    /**
     * Removes expired items from the cache.
     */
    public removeExpiredItems() {
        for (const [key, value] of this.cache.entries()) {
            if (this.isItemExpired(value)) {
                this.cache.delete(key);
            }
        }
        this.saveCache();
    }

    /**
     * Saves the cache to local storage.
     *
     * @param debounceSaveCache - Determines whether to debounce saving the cache.
     */
    public saveCache(debounceSaveCache?: boolean) {
        if (debounceSaveCache == false) this.doSaveCache();
        // Schedule in background to save cache to not block current thread and improve performance
        else this.doSaveCacheDebounce();
    }

    private doSaveCache = () => {
        try {
            const cacheData = JSON.stringify(Array.from(this.cache.entries()));
            localStorage.setItem(this.cacheKey, cacheData);
        } catch (error) {
            console.log('Local Storage is full, Please empty data');
            console.error(error);
            this.clear();
        }
    };

    private doSaveCacheDebounce = task_debounce(() => this.doSaveCache(), this.options.defaultDebounceSaveCacheMs);

    /**
     * Checks if a cached item is expired.
     *
     * @param item - The cached item to check.
     * @returns True if the item is expired, otherwise false.
     */
    public isItemExpired(item: PlatformCachingItem) {
        const ttl = item.ttl ?? this.options.ttl;
        return Date.now() - item.timestamp >= ttl * 1000;
    }

    /**
     * Gets cached data for a given key.
     *
     * @param key - The key for which to retrieve the cached data.
     * @param objectConstuctor - Optional constructor function to create an object from the cached data.
     * @returns The cached data or undefined if not found.
     */
    public override get<T>(key: string, objectConstuctor?: (data?: Partial<T>) => T): T | undefined {
        try {
            const cachedItem = this.cache.get(key);

            if (cachedItem != null) {
                if (this.isItemExpired(cachedItem)) {
                    this.delete(key);
                    return undefined;
                }
                const data = cachedItem.data != null ? JSON.parse(<string>cachedItem.data) : null;

                return objectConstuctor != null ? objectConstuctor(data) : data;
            }
            return undefined;
        } catch (error) {
            console.error(error);
            this.clear();
            return undefined;
        }
    }

    /**
     * Sets cached data for a given key.
     *
     * @param key - The key for which to set the cached data.
     * @param data - The data to be cached.
     * @param options - Additional options for caching, such as time to live (TTL).
     */
    public override set<T>(key: string, data: T | undefined, options?: PlatformCachingServiceSetCacheOptions): void {
        if (data == undefined) this.delete(key);
        else this.doSetData<T>(data, options, key);
    }

    private doSetData<T>(
        data: NonNullable<T>,
        options: PlatformCachingServiceSetCacheOptions | undefined,
        key: string
    ) {
        const serializedData = JSON.stringify(toPlainObj(data));
        const debounceSaveCache =
            options?.debounceSaveCache != undefined
                ? options?.debounceSaveCache
                : this.options.debounceSaveCache ?? true;

        const newItem: PlatformCachingItem = {
            data: serializedData,
            timestamp: Date.now(),
            ttl: options?.ttl
        };

        // If cache is full, delete the oldest item
        if (this.cache.size >= this.options.maxSize) {
            const oldestKey = this.findOldestKey();
            if (oldestKey != null) this.cache.delete(oldestKey);
        }

        this.cache.set(key, newItem);
        this.saveCache(debounceSaveCache);
    }

    public override delete(key: string): void {
        this.cache.delete(key);
        this.saveCache();
    }

    public override clear(): void {
        try {
            this.cache.clear();
            this.saveCache(false);
        } catch (error) {
            console.error(error);
        }
    }

    protected findOldestKey() {
        let oldestKey = null;
        let oldestTimestamp = Infinity;

        for (const [key, value] of this.cache.entries()) {
            if (value.timestamp < oldestTimestamp) {
                oldestKey = key;
                oldestTimestamp = value.timestamp;
            }
        }

        return oldestKey;
    }
}
