import { useState, useRef, useCallback, useEffect } from 'react';
import { useEditorStore } from '../../stores/editor-store';
import { uploadAsset } from '../../lib/storage';

interface Asset {
  uid: string;
  name: string;
  type: 'image' | 'audio' | 'other';
  size: string;
  dataUrl?: string;
  url?: string;
  uploading?: boolean;
  progress?: number;
}

const MAX_FILE_SIZE = 10 * 1024 * 1024;

export function AssetManager() {
  const story = useEditorStore((s) => s.story);
  const [assets, setAssets] = useState<Asset[]>([]);
  const [dragOver, setDragOver] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const mountedRef = useRef(true);

  useEffect(() => {
    return () => { mountedRef.current = false; };
  }, []);

  if (!story) return null;

  const handleFiles = useCallback((files: FileList) => {
    setError(null);
    const fileArray = Array.from(files);

    const oversized = fileArray.filter((f) => f.size > MAX_FILE_SIZE);
    if (oversized.length > 0) {
      setError(`${oversized.length} arquivo(s) excedem 10MB.`);
      return;
    }

    const newAssets: Asset[] = fileArray.map((file) => {
      const type = file.type.startsWith('image/') ? 'image' as const :
        file.type.startsWith('audio/') ? 'audio' as const : 'other' as const;

      const asset: Asset = {
        uid: `${file.name}-${Date.now()}-${Math.random().toString(36).slice(2, 6)}`,
        name: file.name,
        type,
        size: formatSize(file.size),
        uploading: true,
        progress: 0,
      };

      const reader = new FileReader();
      reader.onload = (e) => {
        if (!mountedRef.current) return;
        asset.dataUrl = e.target?.result as string;
        setAssets((prev) => prev.map((a) => (a.uid === asset.uid ? { ...asset } : a)));
      };
      reader.onerror = () => {
        if (!mountedRef.current) return;
        setAssets((prev) => prev.filter((a) => a.uid !== asset.uid));
        setError(`Erro ao ler "${file.name}".`);
      };
      reader.readAsDataURL(file);

      uploadAsset(story.metadata.id, file, (pct) => {
        if (!mountedRef.current) return;
        setAssets((prev) => prev.map((a) => (a.uid === asset.uid ? { ...a, progress: pct } : a)));
      }).then((result) => {
        if (!mountedRef.current) return;
        setAssets((prev) => prev.map((a) => (a.uid === asset.uid ? { ...a, uploading: false, url: result.url, progress: 100 } : a)));
      }).catch(() => {
        if (!mountedRef.current) return;
        setAssets((prev) => prev.map((a) => (a.uid === asset.uid ? { ...a, uploading: false } : a)));
        setError(`Falha ao enviar "${file.name}".`);
      });

      return asset;
    });

    setAssets((prev) => [...prev, ...newAssets]);
  }, [story.metadata.id]);

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(false);
    if (e.dataTransfer.files.length > 0) handleFiles(e.dataTransfer.files);
  };

  const removeAsset = (uid: string) => {
    setAssets((prev) => prev.filter((a) => a.uid !== uid));
  };

  const copyPath = (name: string) => {
    navigator.clipboard.writeText(`stories/${story.metadata.id}/assets/${name}`);
  };

  return (
    <div className="h-full flex flex-col bg-[#1a1a2e]">
      <div className="p-3 bg-[#16213e] border-b border-[#2a2a4a]">
        <h3 className="text-sm font-semibold text-white">Gerenciador de Assets</h3>
        <p className="text-[10px] text-[#6b7280] mt-1">
          Arraste imagens e sons ou clique para fazer upload
        </p>
      </div>

      <div
        className={`flex-1 flex flex-col ${dragOver ? 'bg-[#0f3460]' : ''}`}
        onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
        onDragLeave={() => setDragOver(false)}
        onDrop={handleDrop}
      >
        {error && (
          <div className="mx-4 mt-2 p-2 bg-[#ef444420] border border-[#ef4444] rounded text-xs text-[#ef4444]">
            {error}
            <button onClick={() => setError(null)} className="ml-2 hover:underline">✕</button>
          </div>
        )}

        <div className="p-4">
          <div
            className="border-2 border-dashed border-[#2a2a4a] rounded-lg p-8 text-center cursor-pointer hover:border-[#e94560] transition-colors"
            onClick={() => fileInputRef.current?.click()}
          >
            <div className="text-3xl mb-2">📁</div>
            <p className="text-sm text-[#a0a0b0]">
              {dragOver ? 'Solte os arquivos aqui' : 'Clique ou arraste arquivos'}
            </p>
            <p className="text-[10px] text-[#6b7280] mt-1">
              PNG, WebP, MP3, OGG — máx 10MB por arquivo
            </p>
          </div>
          <input
            ref={fileInputRef}
            type="file"
            multiple
            accept="image/png,image/webp,image/jpeg,audio/mp3,audio/ogg,audio/wav"
            className="hidden"
            onChange={(e) => e.target.files && handleFiles(e.target.files)}
          />
        </div>

        <div className="flex-1 overflow-y-auto px-4 pb-4 space-y-1">
          {assets.length === 0 ? (
            <div className="text-center text-xs text-[#6b7280] py-8">
              Nenhum asset carregado. Os paths serão: stories/{story.metadata.id}/assets/...
            </div>
          ) : (
            assets.map((asset) => (
              <div
                key={asset.uid}
                className="flex items-center gap-3 bg-[#0f3460] rounded p-2 group"
              >
                <span className="text-lg">
                  {asset.type === 'image' ? '🖼️' : asset.type === 'audio' ? '🔊' : '📄'}
                </span>
                <div className="flex-1 min-w-0">
                  <div className="text-xs text-[#e0e0e0] truncate">{asset.name}</div>
                  <div className="text-[10px] text-[#6b7280]">
                    {asset.uploading ? `Enviando... ${asset.progress?.toFixed(0) ?? 0}%` : asset.size}
                  </div>
                  {asset.uploading && (
                    <div className="h-1 bg-[#1a1a2e] rounded mt-1 overflow-hidden">
                      <div
                        className="h-full bg-[#3b82f6] transition-all"
                        style={{ width: `${asset.progress ?? 0}%` }}
                      />
                    </div>
                  )}
                  {asset.url && (
                    <div className="text-[10px] text-[#10b981] mt-0.5">✓ Na nuvem</div>
                  )}
                </div>
                <div className="hidden group-hover:flex items-center gap-1">
                  <button
                    onClick={() => copyPath(asset.name)}
                    className="text-[10px] text-[#3b82f6] hover:text-white px-1.5 py-0.5 rounded bg-[#1a1a2e]"
                    title="Copiar path"
                  >
                    📋 Path
                  </button>
                  <button
                    onClick={() => removeAsset(asset.uid)}
                    className="text-[10px] text-[#ef4444] hover:text-white px-1.5 py-0.5 rounded bg-[#1a1a2e]"
                  >
                    ✕
                  </button>
                </div>
              </div>
            ))
          )}
        </div>
      </div>

      <div className="p-3 bg-[#16213e] border-t border-[#2a2a4a] text-[10px] text-[#6b7280]">
        {assets.length} arquivo{assets.length !== 1 ? 's' : ''} carregado{assets.length !== 1 ? 's' : ''}
        {assets.length > 0 && (
          <span className="ml-2 text-[#a0a0b0]">
            (paths: stories/{story.metadata.id}/assets/...)
          </span>
        )}
      </div>
    </div>
  );
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
