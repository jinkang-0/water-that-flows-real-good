
mod gl_wrap;
mod camera;
mod window_utils;

use glfw::{ Context, PWindow, Glfw, GlfwReceiver, WindowEvent, Key, Action };

use gl_wrap::shader;
use camera::Camera;


pub struct Application {
    camera_uniform_location: shader::UniformLocationMat4,
    test_shader_program: shader::Program,
    test_vao: gl_wrap::VAO,


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

        // other init
        let fragment_src = include_str!("../shaders/test.frag");
        let vertex_src = include_str!("../shaders/test.vert");

        let fragment = shader::Shader::from_str(fragment_src, gl::FRAGMENT_SHADER);
        let vertex = shader::Shader::from_str(vertex_src, gl::VERTEX_SHADER);

        let program = shader::Program::from_shaders([&fragment, &vertex].into_iter());

        let camera_uniform_location = shader::UniformLocationMat4::new(program.get_uniform_location(c"view_proj"));

        // The vertex array object is not really used for anything yet
        // It is here because drawing without creating one is deprecated (the default VAO is depricated)
        let vao = gl_wrap::VAO::new();


        // done initializing, set time to zero
        glfw.set_time(0.0);
        Self {
            glfw,
            main_window,
            event_reciever,
            frame_timer: window_utils::FrameTimer::new(),
            cursor_motion: window_utils::CursorMotionTracker::new(),

            camera: Camera::new(std::f64::consts::PI/3.0, 0.01, 100.0),
            camera_controller: window_utils::CameraControl::default(),

            test_vao: vao,
            test_shader_program: program,
            camera_uniform_location,
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

            unsafe { gl::ClearColor(0.02, 0.02, 0.02, 1.0) };
            unsafe { gl::Clear(gl::COLOR_BUFFER_BIT) };

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
    fn update(&mut self, t: f64, dt: f64) {
        // get the total motion of the mouse over the frame
        let cursor_movement = self.cursor_motion.get_motion();

        // first person camera control
        self.camera_controller.handle_camera_control_input(&self.main_window, &mut self.camera, dt, cursor_movement);

        // print some frame timing info
        println!("Time: {}, Delta Time: {}, Framerate: {}", t, dt, 1.0 / dt);
    }
    fn draw(&mut self) {
        // bind the (empty) vertex array object
        self.test_vao.bind();
        // use the shader
        self.test_shader_program.bind();

        // calculate view and projection
        let view = self.camera.view();
        let proj = self.camera.proj();
        let view_proj = proj * view;

        // upload the view times the projection
        self.camera_uniform_location.upload(&view_proj);

        // draw
        unsafe { gl::DrawArrays(gl::TRIANGLES, 0, 3) };
    }
}

