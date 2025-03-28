
mod gl_wrap;
mod camera;
mod window_utils;

mod meshed_voxel_draw;

use glfw::{ Context, PWindow, Glfw, GlfwReceiver, WindowEvent, Key, Action };

use self::gl_wrap::gl_check_errors;
use camera::Camera;
use crate::application::meshed_voxel_draw::{Voxel, VoxelType};


pub struct Application {
    chunk_draws: Vec<meshed_voxel_draw::VoxelChunkDraw>,
    voxel_draw: meshed_voxel_draw::VoxelDraw,


    camera_controller: window_utils::CameraControl,
    camera: camera::Camera,

    cursor_motion: window_utils::CursorMotionTracker,
    frame_timer: window_utils::FrameTimer,
    // Window Objects
    event_reciever: GlfwReceiver<(f64, WindowEvent)>,
    main_window: PWindow,
    // WARNING: Glfw should be dropped LAST (for safety when destroying any OpenGL objects)
    glfw: Glfw
}

impl Application {
    pub fn new(width: u32, height: u32) -> Self {
        // init GLFW
        let mut glfw = glfw::init(glfw::fail_on_errors).unwrap();
        // create GLFW window
        glfw.window_hint(glfw::WindowHint::ContextVersion(3, 3));
        glfw.window_hint(glfw::WindowHint::OpenGlProfile(glfw::OpenGlProfileHint::Core));
        let (mut main_window, event_reciever) = glfw.create_window(width, height, "Sand Simulation", glfw::WindowMode::Windowed)
            .expect("Failed to create GLFW window.");
        main_window.set_key_polling(true);
        main_window.make_current();
        // allow for cursor movement events
        main_window.set_cursor_pos_polling(true);
        main_window.set_cursor_enter_polling(true);
        // set framerate control method
        glfw.set_swap_interval(glfw::SwapInterval::Adaptive);

        // load OpenGL function pointers
        gl::load_with(|s| main_window.get_proc_address(s) as *const _);

        // print OpenGL version
        let gl_version_cstr = unsafe { std::ffi::CStr::from_ptr(gl::GetString(gl::VERSION) as *const i8) };
        println!("Using OpenGL version: {:?}", gl_version_cstr);

        // OpenGL settings
        unsafe { gl::Enable(gl::DEPTH_TEST) };
        unsafe { gl::ClearColor(0.02, 0.02, 0.02, 1.0) };
        unsafe { gl::ClearDepth(1.0) };


        // other init
        // Create a voxel chunk of size 128x128x128
        let size: isize = 128;
        let mut chunk = meshed_voxel_draw::VoxelChunk::new([size, size, size].into());
        let t0 = glfw.get_time();
        // fill it such that it forms a sphere
        for (p, v) in chunk.iter_voxels_mut() {
            if linalg::dot(&(p + -size/2), &(p + -size/2)) < (size/2)*(size/2) {
                *v = Voxel::new(VoxelType::Sand);
            }
        }
        let t1 = glfw.get_time();

        // create voxel drawer
        let voxel_draw = meshed_voxel_draw::VoxelDraw::new();
        // create the draw for the sphere
        let mut chunk_draws = vec![
            meshed_voxel_draw::VoxelChunkDraw::new(),
        ];
        // generate the mesh for the drawer
        chunk_draws[0].generate(chunk, 0.1);
        let t2 = glfw.get_time();
        println!("Chunk Generation Time: {}", t1 - t0);
        println!("Mesh Generation Time: {}", t2 - t1);


        // done initializing, set time to zero
        glfw.set_time(0.0);
        Self {
            glfw,
            main_window,
            event_reciever,
            frame_timer: window_utils::FrameTimer::new(),
            cursor_motion: window_utils::CursorMotionTracker::new(),

            camera: Camera::new(std::f64::consts::PI/3.0, 0.05, 204.8),
            camera_controller: window_utils::CameraControl::default(),

            voxel_draw,
            chunk_draws,
        }
    }
    pub fn run(&mut self) {
        while !self.main_window.should_close() {
            let t = self.glfw.get_time();
            let dt = self.frame_timer.start_frame(t);

            // handle window input
            self.handle_events();

            // update program state (run simulation)
            self.update(t, dt);

            // update OpenGL viewport and clear screen
            let window_size = self.main_window.get_framebuffer_size();
            unsafe { gl::Viewport(0, 0, window_size.0, window_size.1) };
            self.camera.aspect = (window_size.0 as f64) / (window_size.1 as f64);

            unsafe { gl::Clear(gl::COLOR_BUFFER_BIT | gl::DEPTH_BUFFER_BIT) };

            // draw
            self.draw();

            // present
            self.main_window.swap_buffers();
        }
    }

    fn handle_events(&mut self) {
        // poll for new events
        self.glfw.poll_events();
        // reset the cursor motion tracker
        self.cursor_motion.start_capture();
        // loop through new events
        for (_time, event) in glfw::flush_messages(&self.event_reciever) {
            match event {
                WindowEvent::Key(Key::Escape, _, Action::Press, _) => {
                    // Escape pressed
                    if self.main_window.get_cursor_mode() == glfw::CursorMode::Normal {
                        // if the cursor is normal/visible, exit
                        self.main_window.set_should_close(true);
                    } else {
                        // if the cursor is not, set the mode to normal
                        self.main_window.set_cursor_mode(glfw::CursorMode::Normal);
                        // tag the cursor tracker (because cursor jumps can be registered when the cursor is shown)
                        self.cursor_motion.tag_reset();
                    }
                },
                // hide (and force center) the cursor when enter is pressed
                WindowEvent::Key(Key::Enter, _, Action::Press, _) => {
                    // enter first person camera control when enter is pressed
                    self.main_window.set_cursor_mode(glfw::CursorMode::Disabled);
                },
                WindowEvent::CursorPos(x, y) => {
                    // update the cursor motion tracker (it will keep track of the first and last position of the cursor)
                    self.cursor_motion.cursor_pos_event(x, y);
                },
                // tag the cursor when the mouse enters the edge of the window
                // (see the docstring on `CursorMotionTracker::tag_reset` for mor info
                WindowEvent::CursorEnter(true) => { self.cursor_motion.tag_reset(); },
                _ => {}
            }
        }
    }
    fn update(&mut self, _t: f64, dt: f64) {
        // get the total motion of the mouse over the frame
        let cursor_movement = self.cursor_motion.get_motion();

        // first person camera control
        self.camera_controller.handle_camera_control_input(&self.main_window, &mut self.camera, dt, cursor_movement);
    }
    fn draw(&mut self) {
        // check for any errors from OpenGL
        gl_check_errors(line!(), file!());

        // calculate view and projection
        let view = self.camera.view();
        let proj = self.camera.proj();
        let view_proj = proj * view;


        // set state common across all voxel draw calls
        self.voxel_draw.start_draws(&view_proj);
        // draw each voxel chunk
        for chunk_draw in &self.chunk_draws {
            chunk_draw.draw();
        }
    }
}

