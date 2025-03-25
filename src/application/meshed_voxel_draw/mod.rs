
use linalg::{Matrix, Vector};

use super::gl_wrap::{self, shader};


#[repr(u32)]
#[derive(PartialEq, Eq)]
pub enum VoxelType {
    Empty,
    Sand,
}

pub struct Voxel {
    ty: VoxelType
}
impl Default for Voxel {
    fn default() -> Self {
        Self { ty: VoxelType::Empty }
    }
}
impl Voxel {
    pub fn new(ty: VoxelType) -> Self {
        Self { ty }
    }
    pub fn is_empty(&self) -> bool {
        self.ty == VoxelType::Empty
    }
    pub fn is_opaque(&self) -> bool {
        self.ty != VoxelType::Empty
    }
}

/// A volume of voxel data
pub struct VoxelChunk {
    extent: Vector<isize, 3>,
    default: Voxel,
    data: Vec<Voxel>,
}
impl std::ops::Index<&(isize, isize, isize)> for VoxelChunk {
    type Output = Voxel;
    fn index(&self, pos: &(isize, isize, isize)) -> &Voxel {
        &self.data[self.to_index(&Vector::<isize, 3>::from([pos.0, pos.1, pos.2]))]
    }
}
impl std::ops::IndexMut<&(isize, isize, isize)> for VoxelChunk {
    fn index_mut(&mut self, pos: &(isize, isize, isize)) -> &mut Voxel {
        let index = self.to_index(&Vector::<isize, 3>::from([pos.0, pos.1, pos.2]));
        &mut self.data[index]
    }
}
impl std::ops::Index<(isize, isize, isize)> for VoxelChunk {
    type Output = Voxel;
    fn index(&self, pos: (isize, isize, isize)) -> &Voxel {
        &self[&pos]
    }
}
impl std::ops::IndexMut<(isize, isize, isize)> for VoxelChunk {
    fn index_mut(&mut self, pos: (isize, isize, isize)) -> &mut Voxel {
        &mut self[&pos]
    }
}
impl std::ops::Index<&Vector<isize, 3>> for VoxelChunk {
    type Output = Voxel;
    fn index(&self, pos: &Vector<isize, 3>) -> &Voxel {
        &self.data[self.to_index(pos)]
    }
}
impl std::ops::IndexMut<&Vector<isize, 3>> for VoxelChunk {
    fn index_mut(&mut self, pos: &Vector<isize, 3>) -> &mut Voxel {
        let index = self.to_index(pos);
        &mut self.data[index]
    }
}
impl std::ops::Index<Vector<isize, 3>> for VoxelChunk {
    type Output = Voxel;
    fn index(&self, pos: Vector<isize, 3>) -> &Voxel {
        &self[&pos]
    }
}
impl std::ops::IndexMut<Vector<isize, 3>> for VoxelChunk {
    fn index_mut(&mut self, pos: Vector<isize, 3>) -> &mut Voxel {
        &mut self[&pos]
    }
}
impl VoxelChunk {
    pub fn new(extent: Vector<isize, 3>) -> Self {
        let mut data = Vec::with_capacity((extent.x() * extent.y() * extent.z()).try_into().expect("Invalid extent provided when creating `VoxelChunk`"));
        for _ in 0..(extent.x() * extent.y() * extent.z()) {
            data.push(Voxel::default());
        }
        Self {
            extent,
            default: Voxel::default(),
            data,
        }
    }
    fn to_index(&self, pos: &Vector<isize, 3>) -> usize {
        (
            (pos.z() * self.extent.y() + pos.y()) * self.extent.x() + pos.x()
        ).try_into().unwrap()
    }
    pub fn get_or_default(&self, i: &Vector<isize, 3>) -> &Voxel {
        if i.x() >= 0 && i.x() < self.extent.x() &&
           i.y() >= 0 && i.y() < self.extent.y() &&
           i.z() >= 0 && i.z() < self.extent.z()
        {
            &self[i]
        } else {
            &self.default
        }
    }
    pub fn iter_voxels(&self) -> impl Iterator<Item = (Vector<isize, 3>, &Voxel)> {
        self.data.iter().enumerate().map(|(i, v)| {
            let i = isize::try_from(i).unwrap();
            let x = i % self.extent.x();
            let y = (i / self.extent.x()) % self.extent.y();
            let z = (i / (self.extent.x() * self.extent.y())) % self.extent.z();
            ([x, y, z].into(), v)
        })
    }
    pub fn iter_voxels_mut(&mut self) -> impl Iterator<Item = (Vector<isize, 3>, &mut Voxel)> {
        self.data.iter_mut().enumerate().map(|(i, v)| {
            let i = isize::try_from(i).unwrap();
            let x = i % self.extent.x();
            let y = (i / self.extent.x()) % self.extent.y();
            let z = (i / (self.extent.x() * self.extent.y())) % self.extent.z();
            ([x, y, z].into(), v)
        })
    }
}

/// Structure for keeping information that shared across all chunk draws
pub struct VoxelDraw {
    shader_program: shader::Program,
    view_proj_uniform_loc: shader::UniformLocationMat4,
}
impl VoxelDraw {
    pub fn new() -> Self {
        let vertex_src = include_str!("../../shaders/meshed_voxel.vert");
        let fragment_src = include_str!("../../shaders/meshed_voxel.frag");

        let vertex = shader::Shader::from_str(vertex_src, gl::VERTEX_SHADER);
        let fragment = shader::Shader::from_str(fragment_src, gl::FRAGMENT_SHADER);

        let program = shader::Program::from_shaders([&fragment, &vertex].into_iter());

        let view_proj_uniform_loc = shader::UniformLocationMat4::new(program.get_uniform_location(c"view_proj"));

        Self {
            shader_program: program,
            view_proj_uniform_loc,
        }
    }
    /// Set state for subsiquent voxel chunk draws
    /// (also uploads view projection matrix)
    pub fn start_draws(&self, view_proj: &Matrix<f32, 4, 4>) {
        unsafe { gl::Enable(gl::CULL_FACE) };
        unsafe { gl::CullFace(gl::BACK) };
        unsafe { gl::FrontFace(gl::CCW) };
        self.shader_program.bind();
        self.view_proj_uniform_loc.upload(view_proj);
    }
}

/// The data for a single vertex for the voxel mesh
#[derive(Copy, Clone)]
#[repr(C)]
struct VoxelDrawVertex {
    pos: Vector<f32, 3>,
    normal: Vector<f32, 3>,
}

/// Structure for keeping information for a single chunk draw
pub struct VoxelChunkDraw {
    vao: gl_wrap::VAO,
    vbo: gl_wrap::VBO,
    // number of vertices to draw
    num_draw: i32,
}
impl VoxelChunkDraw {
    pub fn new() -> Self {
        let vao = gl_wrap::VAO::new();
        Self {
            vao,
            vbo: gl_wrap::VBO::new(),
            num_draw: 0,
        }
    }
    pub fn generate(&mut self, chunk: VoxelChunk, voxel_size: f32) {
        self.vao.bind();
        self.vbo.bind(gl::ARRAY_BUFFER);
        // faces for a cube
        // (each face here is made of two triangles, each triangle of 3 vertices)
        let faces: [[VoxelDrawVertex; 6]; 6] = [[
            VoxelDrawVertex { pos: [ 0.0, 0.0, 0.0 ].into(), normal: [ 0.0, 0.0,-1.0].into(), },
            VoxelDrawVertex { pos: [ 1.0, 1.0, 0.0 ].into(), normal: [ 0.0, 0.0,-1.0].into(), },
            VoxelDrawVertex { pos: [ 1.0, 0.0, 0.0 ].into(), normal: [ 0.0, 0.0,-1.0].into(), },
            VoxelDrawVertex { pos: [ 0.0, 0.0, 0.0 ].into(), normal: [ 0.0, 0.0,-1.0].into(), },
            VoxelDrawVertex { pos: [ 0.0, 1.0, 0.0 ].into(), normal: [ 0.0, 0.0,-1.0].into(), },
            VoxelDrawVertex { pos: [ 1.0, 1.0, 0.0 ].into(), normal: [ 0.0, 0.0,-1.0].into(), },
        ],[                                                            
            VoxelDrawVertex { pos: [ 0.0, 0.0, 1.0 ].into(), normal: [ 0.0, 0.0, 1.0].into(), },
            VoxelDrawVertex { pos: [ 1.0, 0.0, 1.0 ].into(), normal: [ 0.0, 0.0, 1.0].into(), },
            VoxelDrawVertex { pos: [ 1.0, 1.0, 1.0 ].into(), normal: [ 0.0, 0.0, 1.0].into(), },
            VoxelDrawVertex { pos: [ 0.0, 0.0, 1.0 ].into(), normal: [ 0.0, 0.0, 1.0].into(), },
            VoxelDrawVertex { pos: [ 1.0, 1.0, 1.0 ].into(), normal: [ 0.0, 0.0, 1.0].into(), },
            VoxelDrawVertex { pos: [ 0.0, 1.0, 1.0 ].into(), normal: [ 0.0, 0.0, 1.0].into(), },
        ],[                                                            
            VoxelDrawVertex { pos: [ 0.0, 0.0, 0.0 ].into(), normal: [ 0.0,-1.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 1.0, 0.0, 0.0 ].into(), normal: [ 0.0,-1.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 1.0, 0.0, 1.0 ].into(), normal: [ 0.0,-1.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 0.0, 0.0, 0.0 ].into(), normal: [ 0.0,-1.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 1.0, 0.0, 1.0 ].into(), normal: [ 0.0,-1.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 0.0, 0.0, 1.0 ].into(), normal: [ 0.0,-1.0, 0.0].into(), },
        ],[                                                            
            VoxelDrawVertex { pos: [ 0.0, 1.0, 0.0 ].into(), normal: [ 0.0, 1.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 1.0, 1.0, 1.0 ].into(), normal: [ 0.0, 1.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 1.0, 1.0, 0.0 ].into(), normal: [ 0.0, 1.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 0.0, 1.0, 0.0 ].into(), normal: [ 0.0, 1.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 0.0, 1.0, 1.0 ].into(), normal: [ 0.0, 1.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 1.0, 1.0, 1.0 ].into(), normal: [ 0.0, 1.0, 0.0].into(), },
        ],[                                                            
            VoxelDrawVertex { pos: [ 0.0, 0.0, 0.0 ].into(), normal: [-1.0, 0.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 0.0, 1.0, 1.0 ].into(), normal: [-1.0, 0.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 0.0, 1.0, 0.0 ].into(), normal: [-1.0, 0.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 0.0, 0.0, 0.0 ].into(), normal: [-1.0, 0.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 0.0, 0.0, 1.0 ].into(), normal: [-1.0, 0.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 0.0, 1.0, 1.0 ].into(), normal: [-1.0, 0.0, 0.0].into(), },
        ],[                                                            
            VoxelDrawVertex { pos: [ 1.0, 0.0, 0.0 ].into(), normal: [ 1.0, 0.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 1.0, 1.0, 0.0 ].into(), normal: [ 1.0, 0.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 1.0, 1.0, 1.0 ].into(), normal: [ 1.0, 0.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 1.0, 0.0, 0.0 ].into(), normal: [ 1.0, 0.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 1.0, 1.0, 1.0 ].into(), normal: [ 1.0, 0.0, 0.0].into(), },
            VoxelDrawVertex { pos: [ 1.0, 0.0, 1.0 ].into(), normal: [ 1.0, 0.0, 0.0].into(), },
        ],];
        // later closure uses `chunk`, but has to be a move closure
        // bind a reference here so it can move the reference without moving `chunk`
        let chunk_ref = &chunk;
        let verts: Vec<VoxelDrawVertex> = chunk.iter_voxels()
            // iterate through each voxel that is not empty
            .filter(|(_, v)| !v.is_empty()).map(|(p, _)| {
                // get the faces for the cube
                // zip with `di` where the index `p+di` points to the voxel adjacent to that face
                // `di` is equal to the normal of the face
                [
                    (faces[0], Vector::from([ 0, 0,-1])),
                    (faces[1], Vector::from([ 0, 0, 1])),
                    (faces[2], Vector::from([ 0,-1, 0])),
                    (faces[3], Vector::from([ 0, 1, 0])),
                    (faces[4], Vector::from([-1, 0, 0])),
                    (faces[5], Vector::from([ 1, 0, 0])),
                ].into_iter().filter(move |(_, di)| {
                    // filter any faces that face an opaque voxel (they will always be occluded)
                    !chunk_ref.get_or_default(&(p + *di)).is_opaque()
                }).map(move |(face, _)| {
                    face.map(|vertex| {
                        let mut v = vertex.clone();
                        // translate the face to the voxel position
                        v.pos += Vector::<f32, 3>::from(p);
                        // scale everything
                        v.pos *= voxel_size;
                        v
                    })
                })
            })
        // flatten array of face arrays
        // then flatten array of faces to vertices
        .flatten().flatten().collect();

        // get number of vertices to draw
        self.num_draw = verts.len().try_into().unwrap();
        // upload the vertex data to the GPU
        self.vbo.upload_data(&verts, gl::STREAM_DRAW);
        // setup vertex attribute pointers
        let vertex_size = std::mem::size_of::<VoxelDrawVertex>();
        // position attribute (location = 0)
        unsafe { gl::VertexAttribPointer(0, 3, gl::FLOAT, gl::FALSE, i32::try_from(vertex_size).unwrap(), std::mem::offset_of!(VoxelDrawVertex, pos) as *const std::ffi::c_void) };
        unsafe { gl::EnableVertexAttribArray(0) };
        // normal attribute (location = 1)
        unsafe { gl::VertexAttribPointer(1, 3, gl::FLOAT, gl::FALSE, i32::try_from(vertex_size).unwrap(), std::mem::offset_of!(VoxelDrawVertex, normal) as *const std::ffi::c_void) };
        unsafe { gl::EnableVertexAttribArray(1) };
    }
    /// Draw this chunk
    pub fn draw(&self) {
        if self.num_draw != 0 {
            self.vao.bind();
            unsafe { gl::DrawArrays(gl::TRIANGLES, 0, self.num_draw) };
        }
    }
}

