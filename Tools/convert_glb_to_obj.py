import argparse
import struct
from pathlib import Path
from typing import List, Sequence, Tuple

from pygltflib import GLTF2


COMPONENT_TYPE_FORMAT = {
    5120: ("b", 1),  # BYTE
    5121: ("B", 1),  # UNSIGNED_BYTE
    5122: ("h", 2),  # SHORT
    5123: ("H", 2),  # UNSIGNED_SHORT
    5125: ("I", 4),  # UNSIGNED_INT
    5126: ("f", 4),  # FLOAT
}

ACCESSOR_TYPE_SIZE = {
    "SCALAR": 1,
    "VEC2": 2,
    "VEC3": 3,
    "VEC4": 4,
    "MAT2": 4,
    "MAT3": 9,
    "MAT4": 16,
}


def read_accessor_data(gltf: GLTF2, accessor_index: int) -> List[Tuple[float, ...]]:
    accessor = gltf.accessors[accessor_index]
    buffer_view = gltf.bufferViews[accessor.bufferView]
    fmt_char, component_size = COMPONENT_TYPE_FORMAT[accessor.componentType]
    component_count = ACCESSOR_TYPE_SIZE[accessor.type]
    item_size = component_size * component_count

    blob = gltf.binary_blob()
    base_offset = (buffer_view.byteOffset or 0) + (accessor.byteOffset or 0)
    stride = buffer_view.byteStride or item_size

    out: List[Tuple[float, ...]] = []
    unpack_fmt = "<" + (fmt_char * component_count)
    for i in range(accessor.count):
        offset = base_offset + i * stride
        chunk = blob[offset : offset + item_size]
        values = struct.unpack(unpack_fmt, chunk)
        out.append(tuple(float(v) for v in values))
    return out


def flatten_indices(index_data: Sequence[Tuple[float, ...]]) -> List[int]:
    return [int(row[0]) for row in index_data]


def write_obj(
    output_path: Path,
    vertices: Sequence[Tuple[float, ...]],
    uvs: Sequence[Tuple[float, ...]],
    normals: Sequence[Tuple[float, ...]],
    faces: Sequence[Tuple[Tuple[int, int, int], Tuple[int, int, int], Tuple[int, int, int]]],
    has_uv: bool,
    has_normal: bool,
) -> None:
    lines: List[str] = []
    for v in vertices:
        lines.append(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}")
    for uv in uvs:
        # OBJ v axis is usually flipped vs glTF.
        lines.append(f"vt {uv[0]:.6f} {1.0 - uv[1]:.6f}")
    for n in normals:
        lines.append(f"vn {n[0]:.6f} {n[1]:.6f} {n[2]:.6f}")

    for tri in faces:
        parts: List[str] = []
        for v_idx, vt_idx, vn_idx in tri:
            if has_uv and has_normal:
                parts.append(f"{v_idx}/{vt_idx}/{vn_idx}")
            elif has_uv:
                parts.append(f"{v_idx}/{vt_idx}")
            elif has_normal:
                parts.append(f"{v_idx}//{vn_idx}")
            else:
                parts.append(f"{v_idx}")
        lines.append("f " + " ".join(parts))

    output_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def convert(input_path: Path, output_path: Path) -> None:
    gltf = GLTF2().load(str(input_path))
    if not gltf.meshes:
        raise ValueError("No mesh found in GLB.")

    mesh = gltf.meshes[0]

    vertices: List[Tuple[float, ...]] = []
    uvs: List[Tuple[float, ...]] = []
    normals: List[Tuple[float, ...]] = []
    faces: List[Tuple[Tuple[int, int, int], Tuple[int, int, int], Tuple[int, int, int]]] = []

    has_any_uv = False
    has_any_normal = False

    for primitive in mesh.primitives:
        if primitive.mode is not None and primitive.mode != 4:
            # Exporter currently supports triangles only.
            continue

        positions = read_accessor_data(gltf, primitive.attributes.POSITION)
        primitive_normals: List[Tuple[float, ...]] = []
        primitive_uvs: List[Tuple[float, ...]] = []

        normal_accessor = getattr(primitive.attributes, "NORMAL", None)
        if normal_accessor is not None:
            primitive_normals = read_accessor_data(gltf, normal_accessor)
            has_any_normal = True

        uv_accessor = getattr(primitive.attributes, "TEXCOORD_0", None)
        if uv_accessor is not None:
            primitive_uvs = read_accessor_data(gltf, uv_accessor)
            has_any_uv = True

        if primitive.indices is not None:
            index_rows = read_accessor_data(gltf, primitive.indices)
            indices = flatten_indices(index_rows)
        else:
            indices = list(range(len(positions)))

        if len(indices) % 3 != 0:
            # Keep export robust for imperfect data.
            indices = indices[: len(indices) - (len(indices) % 3)]

        v_base = len(vertices) + 1
        vt_base = len(uvs) + 1
        vn_base = len(normals) + 1

        vertices.extend(positions)
        if primitive_uvs:
            uvs.extend(primitive_uvs)
        if primitive_normals:
            normals.extend(primitive_normals)

        for i in range(0, len(indices), 3):
            tri_indices = indices[i : i + 3]
            tri: List[Tuple[int, int, int]] = []
            for local_idx in tri_indices:
                v_idx = v_base + local_idx
                vt_idx = vt_base + local_idx if primitive_uvs else 0
                vn_idx = vn_base + local_idx if primitive_normals else 0
                tri.append((v_idx, vt_idx, vn_idx))
            faces.append((tri[0], tri[1], tri[2]))

    if not vertices or not faces:
        raise ValueError("No triangle mesh could be exported.")

    output_path.parent.mkdir(parents=True, exist_ok=True)
    write_obj(
        output_path=output_path,
        vertices=vertices,
        uvs=uvs,
        normals=normals,
        faces=faces,
        has_uv=has_any_uv,
        has_normal=has_any_normal,
    )


def main() -> None:
    parser = argparse.ArgumentParser(description="Convert a GLB mesh to OBJ.")
    parser.add_argument("input", type=Path, help="Input .glb path")
    parser.add_argument("output", type=Path, help="Output .obj path")
    args = parser.parse_args()
    convert(args.input, args.output)


if __name__ == "__main__":
    main()
