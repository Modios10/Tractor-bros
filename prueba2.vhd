library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity de10lite is
    Port (  
        boton        : in  std_logic;
        boton2       : in  std_logic;
        clk          : in  std_logic; 
        
        uart_tx      : out std_logic;
        uart_rx      : in  std_logic;

        -- Segmentos (8 pines)
        dB, dF, dA, dG, dC, dDP, dD, dE : OUT std_logic;
        
        -- Los 4 pines que actúan como "Ground" (Multiplexación)
        dig1, dig2, dig3, dig4          : OUT std_logic
    );
end de10lite;

architecture rtl of de10lite is

    component adc0 is
        port (
            CLOCK : in  std_logic := 'X'; 
            RESET : in  std_logic := 'X'; 
            CH0   : out std_logic_vector(11 downto 0);
            CH1   : out std_logic_vector(11 downto 0);
            CH2   : out std_logic_vector(11 downto 0);
            CH3   : out std_logic_vector(11 downto 0);
            CH4   : out std_logic_vector(11 downto 0);
            CH5   : out std_logic_vector(11 downto 0);
            CH6   : out std_logic_vector(11 downto 0);
            CH7   : out std_logic_vector(11 downto 0)
        );
    end component adc0;

    signal btn1_clean, btn2_clean : std_logic;
    signal send_flag, busy        : std_logic := '0';
    signal data_to_send           : std_logic_vector(7 downto 0);
    signal timer_pulse            : std_logic;
    signal raw_x, raw_y           : std_logic_vector(11 downto 0);
    signal x_val_stored, y_val_stored, buttons_stored : std_logic_vector(7 downto 0);

    signal rx_data    : std_logic_vector(7 downto 0);
    signal rx_ready   : std_logic;
    
    -- RX State Machine: IDLE -> TENS -> ONES -> (wait for newline) -> IDLE
    type rx_state_type is (RX_IDLE, RX_TENS, RX_ONES);
    signal rx_state : rx_state_type := RX_IDLE;

    signal reg0, reg1 : unsigned(3 downto 0) := (others => '0');  -- Granos: decenas y unidades

    signal refresh_cnt       : integer range 0 to 50_000 := 0; 
    signal digit_sel         : unsigned(1 downto 0) := "00";
    signal current_digit_val : unsigned(3 downto 0);
    signal segments          : std_logic_vector(6 downto 0);

    type joy_state_type is (IDLE, SEND_HEADER, SEND_X, SEND_Y, SEND_BUTTONS, WAIT_TX);
    signal joy_state : joy_state_type := IDLE;
    signal next_state : joy_state_type := IDLE;

begin

    u0 : component adc0
        port map (
            CLOCK => clk,
            RESET => '0', 
            CH0   => raw_x,
            CH1   => raw_y,
            CH2   => open,
            CH3   => open,
            CH4   => open,
            CH5   => open,
            CH6   => open,
            CH7   => open
        );

    DB1: entity work.debounce port map(clk => clk, sig_in => boton, sig_stable => btn1_clean);
    DB2: entity work.debounce port map(clk => clk, sig_in => boton2, sig_stable => btn2_clean);
    TIMER: entity work.timer_1sec generic map(CLK_FREQ => 1_000_000) port map(clk => clk, pulse => timer_pulse);
    
    TX_UNIT: entity work.uart_tx port map(clk => clk, start => send_flag, data_in => data_to_send, tx_line => uart_tx, busy => busy);
    RX_UNIT: entity work.uart_rx port map(clk => clk, rx_line => uart_rx, data_out => rx_data, new_data_tick => rx_ready);

    -- 1. PROCESO DE RECEPCIÓN - STATE MACHINE (FIXED)
    process(clk)
    begin
        if rising_edge(clk) then
            if rx_ready = '1' then
                case rx_state is
                    when RX_IDLE =>
                        -- Esperando primer dígito (decenas)
                        if rx_data >= x"30" and rx_data <= x"39" then
                            reg1 <= unsigned(rx_data(3 downto 0));
                            rx_state <= RX_TENS;
                        end if;
                        -- Si llega newline u otro carácter, mantenerse en IDLE
                    
                    when RX_TENS =>
                        -- Esperando segundo dígito (unidades)
                        if rx_data >= x"30" and rx_data <= x"39" then
                            reg0 <= unsigned(rx_data(3 downto 0));
                            rx_state <= RX_ONES;
                        elsif rx_data = x"0A" or rx_data = x"0D" then
                            -- Newline sin unidades - reiniciar pero mantener decenas
                            rx_state <= RX_IDLE;
                        end if;
                    
                    when RX_ONES =>
                        -- Par completado. Esperar newline para volver a IDLE.
                        -- Ignorar dígitos adicionales.
                        if rx_data = x"0A" or rx_data = x"0D" then
                            rx_state <= RX_IDLE;
                        end if;
                        -- Si llega otro dígito aquí, simplemente ignorarlo (sin cambio de estado)
                end case;
            end if;
        end if;
    end process;

    -- 2. TRANSMISIÓN DEL JOYSTICK A UNITY
    process(clk)
    begin
        if rising_edge(clk) then
            send_flag <= '0';

            case joy_state is
                when IDLE =>
                    if timer_pulse = '1' then
                        x_val_stored <= raw_x(11 downto 4);
                        y_val_stored <= raw_y(11 downto 4);
                        buttons_stored(7 downto 2) <= "000000";
                        buttons_stored(1) <= not btn2_clean;
                        buttons_stored(0) <= not btn1_clean;
                        joy_state <= SEND_HEADER;
                    end if;

                when SEND_HEADER =>
                    if busy = '0' then
                        data_to_send <= x"FF";
                        send_flag <= '1';
                        joy_state <= WAIT_TX;
                        next_state <= SEND_X;
                    end if;

                when SEND_X =>
                    if busy = '0' then
                        data_to_send <= x_val_stored;
                        send_flag <= '1';
                        joy_state <= WAIT_TX;
                        next_state <= SEND_Y;
                    end if;

                when SEND_Y =>
                    if busy = '0' then
                        data_to_send <= y_val_stored;
                        send_flag <= '1';
                        joy_state <= WAIT_TX;
                        next_state <= SEND_BUTTONS;
                    end if;

                when SEND_BUTTONS =>
                    if busy = '0' then
                        data_to_send <= buttons_stored;
                        send_flag <= '1';
                        joy_state <= WAIT_TX;
                        next_state <= IDLE;
                    end if;

                when WAIT_TX =>
                    if busy = '0' then
                        joy_state <= next_state;
                    end if;

                when others =>
                    joy_state <= IDLE;
            end case;
        end if;
    end process;

    -- 2. REFRESH DE MULTIPLEXACIÓN (1 kHz)
    process(clk)
    begin
        if rising_edge(clk) then
            if refresh_cnt = 50_000 then
                refresh_cnt <= 0;
                digit_sel <= digit_sel + 1;
            else
                refresh_cnt <= refresh_cnt + 1;
            end if;
        end if;
    end process;

    -- 3. MUX DE GRANOS: dig1=decenas, dig2=unidades, dig3=apagado, dig4=apagado
    process(digit_sel, reg0, reg1)
    begin
        case digit_sel is
            when "00" => 
                -- Dígito 1: decenas de granos
                current_digit_val <= reg1;
                dig1 <= '0'; dig2 <= '1'; dig3 <= '1'; dig4 <= '1'; 
            when "01" => 
                -- Dígito 2: unidades de granos
                current_digit_val <= reg0;
                dig1 <= '1'; dig2 <= '0'; dig3 <= '1'; dig4 <= '1'; 
            when "10" => 
                -- Dígito 3: apagado
                current_digit_val <= "1111";
                dig1 <= '1'; dig2 <= '1'; dig3 <= '0'; dig4 <= '1'; 
            when "11" => 
                -- Dígito 4: apagado
                current_digit_val <= "1111";
                dig1 <= '1'; dig2 <= '1'; dig3 <= '1'; dig4 <= '0'; 
            when others => 
                current_digit_val <= "1111";
        end case;
    end process;

    -- 4. DECODIFICADOR USANDO BINARIO ESTRICTO (Para evitar el error del x"0")
    process(current_digit_val)
    begin
        case current_digit_val is
            when "0000" => segments <= "1111110"; -- abcdefg (0)
            when "0001" => segments <= "0110000"; -- 1
            when "0010" => segments <= "1101101"; -- 2
            when "0011" => segments <= "1111001"; -- 3
            when "0100" => segments <= "0110011"; -- 4
            when "0101" => segments <= "1011011"; -- 5
            when "0110" => segments <= "1011111"; -- 6
            when "0111" => segments <= "1110000"; -- 7
            when "1000" => segments <= "1111111"; -- 8
            when "1001" => segments <= "1111011"; -- 9
            when others => segments <= "0000000"; -- Todo apagado
        end case;
    end process;

    -- 5. MAPEO A PINES FÍSICOS
    dA <= segments(6); dB <= segments(5); dC <= segments(4); dD <= segments(3);
    dE <= segments(2); dF <= segments(1); dG <= segments(0); dDP <= '0';

end rtl;
