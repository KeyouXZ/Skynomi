export class GenericCache {
    private cache = new Map<string, unknown>();

    set<T>(key: string, value: T): void {
        this.cache.set(key, value);
    }

    get<T>(key: string): T | undefined {
        return this.cache.get(key) as T | undefined;
    }

    getAllKeys(): string[] {
        return Array.from(this.cache.keys());
    }

    has(key: string): boolean {
        return this.cache.has(key);
    }

    delete(key: string): boolean {
        return this.cache.delete(key);
    }

    clear(): void {
        this.cache.clear();
    }

}
